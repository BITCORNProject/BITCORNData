using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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
    [ServiceFilter(typeof(LockUserAttribute))]
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        const int BitcornHubPK = 196;
        public int TimeToClaimTipMinutes { get; set; } = 60 * 24;
        private readonly BitcornContext _dbContext;

        public TxController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost("rain")]
        public async Task<ActionResult<TxReceipt[]>> Rain([FromBody] RainRequest rainRequest)
        {
            if (rainRequest == null) throw new ArgumentNullException();
            if (rainRequest.From == null) throw new ArgumentNullException();
            if (rainRequest.To == null) throw new ArgumentNullException();
            if (rainRequest.Amount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);

            var processInfo = await TxUtils.ProcessRequest(rainRequest, _dbContext);
            var transactions = processInfo.Transactions;
            if (transactions != null && transactions.Length > 0)
            {
                StringBuilder sql = new StringBuilder();
                if (processInfo.WriteTransactionOutput(sql)) 
                {
                    string pk = nameof(UserStat.UserId);
                    string table = nameof(UserStat);
                    var recipients = processInfo.ValidRecipients;
                    var fromId = transactions[0].From.UserId;

                    var recipientStats = new List<ColumnValuePair>();
                    recipientStats.Add(new ColumnValuePair(nameof(UserStat.RainedOn), 1));
                    recipientStats.Add(new ColumnValuePair(nameof(UserStat.RainedOnTotal), rainRequest.Amount));

                    sql.Append(TxUtils.ModifyNumbers(table, recipientStats, '+', pk, recipients));
                    sql.Append(TxUtils.UpdateNumberIfTop(table,nameof(UserStat.TopRainedOn),rainRequest.Amount,pk,recipients));

                    var senderStats = new List<ColumnValuePair>();
                    senderStats.Add(new ColumnValuePair(nameof(UserStat.Rained), 1));
                    senderStats.Add(new ColumnValuePair(nameof(UserStat.RainTotal), processInfo.TotalAmount));
                    
                    sql.Append(TxUtils.ModifyNumbers(table, senderStats, '+', pk,fromId));
                    sql.Append(TxUtils.UpdateNumberIfTop(table,nameof(UserStat.TopRain),processInfo.TotalAmount,pk,fromId));

                    await _dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                    await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                }
                await TxUtils.AppendTxs(transactions, _dbContext, rainRequest.Columns);

            }
            return processInfo.Transactions;
        }

        [HttpPost("payout")]
        public async Task<int> Payout([FromBody] HashSet<string> chatters)
        {
            var grouping = (await _dbContext.JoinUserModels().Where(u => chatters.Contains(u.UserIdentity.TwitchId))
                .AsNoTracking()
                .ToArrayAsync()).GroupBy(u=>u.Level).ToArray();

            decimal total = 0;
            int changedRows = 0;
            StringBuilder sql = new StringBuilder();

            var pk = nameof(UserWallet.UserId);
            foreach (var group in grouping)
            {
                decimal payout = 0;
                var level = group.Key;
                var ids = group.Select(u => u.UserId).ToArray();
                if (level == "1000")
                {
                    payout = 0.25m;
                }
                else if (level == "2000")
                {
                    payout = .5m;
                }
                else 
                {
                    payout = 1;
                }
                sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), payout, '+', pk, ids));
                sql.Append(TxUtils.ModifyNumber(nameof(UserStat), nameof(UserStat.EarnedIdle), payout, '+', pk, ids));

                int count = await _dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                total += payout * count;
                changedRows += count;
            }
            if (changedRows > 0)
            {
                await _dbContext.Database.ExecuteSqlRawAsync(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), total, '-', pk, BitcornHubPK));
            }
            return changedRows;
        }

        [HttpPost("tipcorn")]
        public async Task<ActionResult<TxReceipt[]>> Tipcorn([FromBody] TipRequest tipRequest)
        {
            if (tipRequest == null) throw new ArgumentNullException();
            if (tipRequest.From == null) throw new ArgumentNullException();
            if (tipRequest.To == null) throw new ArgumentNullException();
            if (tipRequest.To == tipRequest.From) return StatusCode((int)HttpStatusCode.BadRequest);
            if (tipRequest.Amount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);

            try
            {
                var processInfo = await TxUtils.ProcessRequest(tipRequest, _dbContext);
                var transactions = processInfo.Transactions;
                if (transactions != null && transactions.Length > 0)
                {

                    StringBuilder sql = new StringBuilder();
                    if (processInfo.WriteTransactionOutput(sql))
                    {

                        var receipt = transactions[0];
                        if (receipt.Tx != null)
                        {
                            var to = receipt.To.User.UserStat;
                            var from = receipt.From.User.UserStat;
                            var amount = tipRequest.Amount;
                            string table = nameof(UserStat);
                            var pk = nameof(UserStat.UserId);
                            var fromStats = new List<ColumnValuePair>();

                            fromStats.Add(new ColumnValuePair(nameof(UserStat.Tip), 1));
                            fromStats.Add(new ColumnValuePair(nameof(UserStat.TipTotal), amount));

                            sql.Append(TxUtils.ModifyNumbers(table, fromStats, '+', pk, from.UserId));

                            var toStats = new List<ColumnValuePair>();
                            toStats.Add(new ColumnValuePair(nameof(UserStat.Tipped), 1));
                            toStats.Add(new ColumnValuePair(nameof(UserStat.TippedTotal), amount));

                            sql.Append(TxUtils.ModifyNumbers(table, toStats, '+', pk, to.UserId));
                           
                            sql.Append(TxUtils.UpdateNumberIfTop(table, nameof(UserStat.TopTip), amount, pk, from.UserId));
                            sql.Append(TxUtils.UpdateNumberIfTop(table, nameof(UserStat.TopTipped), amount, pk, to.UserId));
                           
                            await _dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                            await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                        }
                    }
                    else
                    {
                        if (processInfo.From != null && !processInfo.From.IsBanned && processInfo.Transactions[0].To == null)
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
                                unclaimed.Expiration = DateTime.Now.AddMinutes(TimeToClaimTipMinutes);
                                unclaimed.Claimed = false;
                                unclaimed.Refunded = false;

                                _dbContext.UnclaimedTx.Add(unclaimed);
                                await _dbContext.Database.ExecuteSqlRawAsync(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), tipRequest.Amount, '-', nameof(UserWallet.UserId), processInfo.From.UserId));
                                await _dbContext.SaveAsync();
                            }
                        }
                    }
                    await TxUtils.AppendTxs(transactions, _dbContext, tipRequest.Columns);

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
