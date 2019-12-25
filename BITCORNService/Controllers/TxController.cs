using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
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
            if (rainRequest == null) throw new ArgumentNullException();
            if (rainRequest.From == null) throw new ArgumentNullException();
            if (rainRequest.To == null) throw new ArgumentNullException();
            var transactions = await TxUtils.ProcessRequest(rainRequest, _dbContext);
            if (transactions != null && transactions.Length > 0)
            {
                var from = await _dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == transactions[0].From.UserId);

                var toHaystack = transactions.Where(t=>t.Tx!=null).Select(t=>t.To.UserId).ToHashSet();
               //poll all users involved in this tx
                await _dbContext.UserStat.Where(u=>toHaystack.Contains(u.UserId)).ToArrayAsync();
                decimal rainAmount = 0;
                for (int i = 0; i < transactions.Length; i++)
                {
                    //find users from cache
                    if (transactions[i].Tx != null)
                    {
                        var stat = _dbContext.UserStat.Find(transactions[i].To.UserId);
                        var amount = transactions[i].Tx.Amount.Value;
                        UpdateStats.RainedOn(stat, amount);
                        rainAmount += amount;
                    }
                }
                UpdateStats.Rain(from,rainAmount);
             
                await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                await TxUtils.AppendTxs(transactions, _dbContext, rainRequest.Columns);

            }
            return transactions;
        }

        [HttpPost("payout")]
        public async Task<int> Payout([FromBody] HashSet<string> chatters)
        {
            var userIdentities = await _dbContext.UserIdentity.Where(u=>chatters.Contains(u.TwitchId)).Select(u=>u.UserId).ToArrayAsync();
            var wallets = await _dbContext.UserWallet.Where(u => userIdentities.Contains(u.UserId)).ToArrayAsync();
            var stats = await _dbContext.UserStat.Where(u => userIdentities.Contains(u.UserId)).ToArrayAsync();
            var users = await _dbContext.User.Where(u=>userIdentities.Contains(u.UserId)).ToArrayAsync();
            decimal total = 0;
            foreach (var userId in userIdentities)
            {
                decimal payout = 0;
                var user = _dbContext.User.Find(userId);
                if (user.IsBanned) continue;
                var wallet = _dbContext.UserWallet.Find(userId);
                var stat = _dbContext.UserStat.Find(userId);
               
                if(user.Level == "1000")
                {
                    payout = 0.25m;
                }
                else if(user.Level == "2000")
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
            var bitcornhub = await _dbContext.UserWallet.FirstOrDefaultAsync(u=>u.UserId==BitcornHubPK);
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
                var transactions = await TxUtils.ProcessRequest(tipRequest, _dbContext);
                if (transactions != null && transactions.Length > 0)
                {
                    var receipt = transactions[0];
                    if (receipt.Tx != null)
                    {
                        var to = await _dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == receipt.To.UserId);

                        var from = await _dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == receipt.From.UserId);
                        UpdateStats.Tip(from, tipRequest.Amount);
                        UpdateStats.Tipped(to, tipRequest.Amount);
                        await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                    }
                    await TxUtils.AppendTxs(transactions, _dbContext, tipRequest.Columns);

                }
                return transactions;
            }
            catch(Exception e)
            {
                throw e;
            }
     
        }

        

        [HttpPost("withdraw")]
        public async Task Withdraw([FromBody] WithdrawUser withdrawUser)
        {
            //sender twitchid, cornaddy, amount
            await TxUtils.ExecuteDebitTx(withdrawUser, _dbContext);

            //call to wallet to properly withdraw TODO

            //tx id TODO
        }



    }
}
