using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    //[ServiceFilter(typeof(LockUserAttribute))]
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        const int BitcornHubPK = 196;
        private readonly BitcornContext _dbContext;

        public TxController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost("rain")]
        public async Task<object> Rain([FromBody] RainRequest rainRequest)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (rainRequest == null) throw new ArgumentNullException();
            if (rainRequest.From == null) throw new ArgumentNullException();
            if (rainRequest.To == null) throw new ArgumentNullException();
            var processInfo = await TxUtils.ProcessRequest(rainRequest, _dbContext);
            var transactions = processInfo.Transactions;
            if (transactions != null && transactions.Length > 0)
            {
                var from = transactions[0].From.User.UserStat;
                decimal rainAmount = 0;
                for (int i = 0; i < transactions.Length; i++)
                {
                    //find users from cache
                    if (transactions[i].Tx != null)
                    {
                        var stat = transactions[i].To.User.UserStat;
                        var amount = transactions[i].Tx.Amount.Value;
                        UpdateStats.RainedOn(stat, amount);
                        rainAmount += amount;
                    }
                }
                UpdateStats.Rain(from, rainAmount);

                await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                await TxUtils.AppendTxs(transactions, _dbContext, rainRequest.Columns);

            }
            sw.Stop();
            System.Diagnostics.Debug.WriteLine(sw.ElapsedMilliseconds);
            return processInfo.Transactions;
        }

        [HttpPost("payout")]
        public async Task<int> Payout([FromBody] HashSet<string> chatters)
        {
            var users = await _dbContext.JoinUserModels().Where(u => chatters.Contains(u.UserIdentity.TwitchId)).ToArrayAsync();
            decimal total = 0;
            foreach (var user in users)
            {
                decimal payout = 0;


                if (user.Level == "1000")
                {
                    payout = 0.25m;
                }
                else if (user.Level == "2000")
                {
                    payout = .5m;
                }
                else if (user.Level == "3000")
                {
                    payout = 1;
                }
                total += payout;
                user.UserWallet.Balance += payout;
                user.UserStat.EarnedIdle += payout;

            }
            var bitcornhub = await _dbContext.UserWallet.FirstOrDefaultAsync(u => u.UserId == BitcornHubPK);
            bitcornhub.Balance -= total;

            return await _dbContext.SaveAsync();
        }

        [HttpPost("tipcorn")]
        public async Task<object> Tipcorn([FromBody] TipRequest tipRequest)
        {
            if (tipRequest == null) throw new ArgumentNullException();
            if (tipRequest.From == null) throw new ArgumentNullException();
            if (tipRequest.To == null) throw new ArgumentNullException();
            if (tipRequest.Amount == 0) throw new ArgumentNullException();

            try
            {
                var processInfo = await TxUtils.ProcessRequest(tipRequest, _dbContext);

                var transactions = processInfo.Transactions;
                if (transactions != null && transactions.Length > 0)
                {
                    var receipt = transactions[0];
                    if (receipt.Tx != null)
                    {
                        var to = receipt.To.User.UserStat;

                        var from = receipt.From.User.UserStat;
                        UpdateStats.Tip(from, tipRequest.Amount);
                        UpdateStats.Tipped(to, tipRequest.Amount);
                        await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                    }
                    await TxUtils.AppendTxs(transactions, _dbContext, tipRequest.Columns);

                }
                else
                {
                    if (processInfo.From != null && !processInfo.From.IsBanned)
                    {
                        if (processInfo.From.UserWallet.Balance >= tipRequest.Amount)
                        {
                            var unclaimed = new UnclaimedTx();
                            unclaimed.TxType = ((ITxRequest)tipRequest).TxType;
                            unclaimed.Platform = tipRequest.Platform;
                            unclaimed.ReceiverPlatformId = BitcornUtils.GetPlatformId(tipRequest.To).Id;
                            unclaimed.Amount = tipRequest.Amount;
                            unclaimed.Timestamp = DateTime.Now;
                            unclaimed.SenderUserId = processInfo.From.UserId;
                            unclaimed.Expiration = DateTime.Now.AddHours(24);
                            unclaimed.Claimed = false;
                            unclaimed.Refunded = false;

                            _dbContext.UnclaimedTx.Add(unclaimed);

                            processInfo.From.UserWallet.Balance -= tipRequest.Amount;
                            await _dbContext.SaveAsync();
                        }
                        //    unclaimed.SenderUser
                    }
                }
                return transactions;
            }
            catch (Exception e)
            {
                throw e;
            }

        }
    }
}
