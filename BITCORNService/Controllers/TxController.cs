using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Platforms;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Twitch;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
namespace BITCORNService.Controllers
{
    [Authorize(Policy = AuthScopes.SendTransaction)]

    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        public int TimeToClaimTipMinutes { get; set; } = 60 * 24;
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public TxController(IConfiguration configuration, BitcornContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("rain")]
        public async Task<ActionResult<TxReceipt[]>> Rain([FromBody] RainRequest rainRequest)
        {
            try
            {
                if (!TxUtils.AreTransactionsEnabled(_configuration))
                {
                    return StatusCode(420);
                }

                //config["Config:IdKey"]
                if (rainRequest == null) throw new ArgumentNullException();
                if (rainRequest.From == null) throw new ArgumentNullException();
                if (rainRequest.To == null) throw new ArgumentNullException();
                if (rainRequest.Amount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);
                rainRequest.FromUser = this.GetCachedUser();
                UserLivestream liveStream = null;
                if (!string.IsNullOrEmpty(rainRequest.IrcTarget))
                {
                    var streamQuery = await _dbContext.UserLivestream
                        .Join(_dbContext.UserIdentity, x => x.UserId, x => x.UserId, (stream, identity) => new
                        {
                            stream,
                            identity
                        }).FirstOrDefaultAsync(x => x.identity.TwitchId == rainRequest.IrcTarget);

                    if (streamQuery != null)
                    {
                        if (!streamQuery.stream.EnableTransactions)
                        {
                            return StatusCode(420);
                        }

                        liveStream = streamQuery.stream;
                    }

                }
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
                        recipientStats.Add(new ColumnValuePair(nameof(UserStat.AmountOfRainsReceived), 1));
                        recipientStats.Add(new ColumnValuePair(nameof(UserStat.TotalReceivedBitcornRains), rainRequest.Amount));

                        sql.Append(TxUtils.ModifyNumbers(table, recipientStats, '+', pk, recipients));
                        sql.Append(TxUtils.UpdateNumberIfTop(table, nameof(UserStat.LargestReceivedBitcornRain), rainRequest.Amount, pk, recipients));

                        var senderStats = new List<ColumnValuePair>();
                        senderStats.Add(new ColumnValuePair(nameof(UserStat.AmountOfRainsSent), 1));
                        senderStats.Add(new ColumnValuePair(nameof(UserStat.TotalSentBitcornViaRains), processInfo.TotalAmount));

                        sql.Append(TxUtils.ModifyNumbers(table, senderStats, '+', pk, fromId));
                        sql.Append(TxUtils.UpdateNumberIfTop(table, nameof(UserStat.LargestSentBitcornRain), processInfo.TotalAmount, pk, fromId));
                        if (!string.IsNullOrEmpty(rainRequest.IrcTarget))
                        {
                            var ircTx = new IrcTarget();
                            ircTx.IrcChannel = rainRequest.IrcTarget;
                            ircTx.TxGroupId = transactions.Where(x => x.Tx != null).FirstOrDefault().Tx.TxGroupId;
                            _dbContext.IrcTarget.Add(ircTx);

                            var liveStreamTable = nameof(UserLivestream);
                            var livestreamKey = nameof(UserLivestream.UserId);
                            //var stream = await _dbContext.UserLivestream.AsNoTracking().FirstOrDefaultAsync(x => x.IrcTarget == rainRequest.IrcTarget);
                            if (liveStream != null)
                            {
                                sql.AppendLine(
                                    TxUtils.ModifyNumber(liveStreamTable, nameof(UserLivestream.AmountOfRainsSent), 1, '+', livestreamKey, liveStream.UserId));

                                sql.AppendLine(
                                   TxUtils.ModifyNumber(liveStreamTable, nameof(UserLivestream.TotalSentBitcornViaRains), processInfo.TotalAmount, '+', livestreamKey, liveStream.UserId));
                            }
                        }

                        await _dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                        await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                    }
                    await TxUtils.AppendTxs(transactions, _dbContext, rainRequest.Columns);

                }
                return processInfo.Transactions;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(rainRequest));
                throw e;
            }
        }

        async Task<int> PerformPayout(UserLivestream stream, User from, IGrouping<string, User>[] grouping, decimal minutes)
        {
            decimal total = 0;
            int totalPayTargets = 0;
            StringBuilder sql = new StringBuilder();

            var pk = nameof(UserWallet.UserId);
            foreach (var group in grouping)
            {
                decimal payout = 0;
                var level = group.Key;
                //if (level == ) continue;
                int[] ids = group.Select(u => u.UserId).Where(u => u != from.UserId).ToArray();
                if (ids.Length == 0) continue;
                if (level == "1000")
                {
                    payout = stream.Tier1IdlePerMinute;//0.25m;
                }
                else if (level == "2000")
                {
                    payout = stream.Tier2IdlePerMinute;//.5m;
                }
                else
                {
                    payout = stream.Tier3IdlePerMinute;//1;
                }

                payout *= minutes;
                sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), payout, '+', pk, ids));
                sql.Append(TxUtils.ModifyNumber(nameof(UserStat), nameof(UserStat.EarnedIdle), payout, '+', pk, ids));
                sql.Append(TxUtils.ModifyNumber(nameof(UserLivestream), nameof(UserLivestream.TotalBitcornPaidViaIdling), payout, '+', nameof(UserLivestream.UserId), new int[] {
                  from.UserId
                }));

                int count = ids.Length;//await _dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                total += payout * count;
                totalPayTargets += count;
                if (from.UserWallet.Balance < total) return 0;
            }
            if (!stream.BitcornhubFunded)
            {
                if (totalPayTargets > 0 && from.UserWallet.Balance >= total)
                {
                    sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), total, '-', pk, from.UserId));
                    var date = DateTime.Now;
                    sql.Append($" UPDATE [{nameof(UserLivestream)}] set [{nameof(UserLivestream.LastSubTickTimestamp)}] = '{date.ToString("yyyy-MM-dd HH:mm:ss.fff")}' where [{nameof(UserLivestream)}].{nameof(UserLivestream.UserId)}={from.UserId} ");
                    var val = await _dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                    return val;
                }
            }
            else
            {
                var bitcornhub = await _dbContext.JoinUserModels().Where(x => x.UserId == TxUtils.BitcornHubPK).FirstOrDefaultAsync();
                if (totalPayTargets > 0 && bitcornhub.UserWallet.Balance >= total)
                {
                    sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), total, '-', pk, bitcornhub.UserId));
                    //sql.Append(TxUtils.ModifyNumber(nameof(UserLivestream), nameof(UserWallet.Balance), total, '-', pk, bitcornhub.UserId));
                    var date = DateTime.Now;
                    sql.Append($" UPDATE [{nameof(UserLivestream)}] set [{nameof(UserLivestream.LastSubTickTimestamp)}] = '{date.ToString("yyyy-MM-dd HH:mm:ss.fff")}' where [{nameof(UserLivestream)}].{nameof(UserLivestream.UserId)}={from.UserId} ");
                    var val = await _dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                    return val;
                }
            }

            return 0;
        }

        [HttpPost("payout")]
        public async Task<ActionResult<object>> Payout([FromBody] PayoutRequest request)
        {
            try
            {
                if (!TxUtils.AreTransactionsEnabled(_configuration))
                {
                    return StatusCode(420);
                }

                if (string.IsNullOrEmpty(request.IrcTarget))
                {
                    return StatusCode(400);
                }

                var streamQuery = await _dbContext.UserLivestream
                        .Join(_dbContext.UserIdentity, x => x.UserId, x => x.UserId, (stream, identity) => new
                        {
                            stream,
                            identity
                        }).FirstOrDefaultAsync(x => x.identity.TwitchId == request.IrcTarget);

                int ret = -1;
                if (streamQuery != null && streamQuery.stream.IrcEventPayments)
                {
                    var userIdentity = streamQuery.identity;
                    var user = await _dbContext.JoinUserModels().Where(x => x.UserIdentity.TwitchId == userIdentity.TwitchId).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        if (string.IsNullOrEmpty(user.UserIdentity.TwitchRefreshToken)) return new { msg = "not valid refresh token" };

                        var twitchAccessToken = await TwitchPlatform.RefreshToken(_dbContext, userIdentity, _configuration);
                        if (string.IsNullOrEmpty(twitchAccessToken))
                            return new
                            {
                                mgs = "failed to fetch twitch access token"
                            };
                        var twitchApi = new Helix(_configuration, _dbContext, twitchAccessToken);
                        var subs = await twitchApi.GetSubs(request.IrcTarget);
                        //var existingSubs = subs.Select(x=>x.Key).ToDictionary;
                        var existingSubs = subs.Where(x => request.Chatters.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                        var existingSubsCheck = existingSubs.Keys.ToHashSet();
                        if (existingSubs.Count > 0)
                        {
                            var groupedUsers = (await _dbContext.JoinUserModels().Where(u => existingSubsCheck.Contains(u.UserIdentity.TwitchId) && !u.IsBanned)
                            .AsNoTracking()
                            .ToArrayAsync()).GroupBy(u => existingSubs[u.UserIdentity.TwitchId]).ToArray();//u.).ToArray();

                            if (UserLockCollection.Lock(user))
                            {
                                try
                                {
                                    ret = await PerformPayout(streamQuery.stream, user, groupedUsers, request.Minutes);
                                
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    await BITCORNLogger.LogError(_dbContext, ex, "");
                                    throw ex;
                                }
                                finally
                                {
                                    UserLockCollection.Release(user);
                                }
                            }
                            else
                            {
                                Console.WriteLine("locked!");
                                return new { msg = "user locked" };
                            }
                        }
                        else
                        {
                            return new { msg = "existing subs count 0 " };//, token = twitchAccessToken, chatters=request.Chatters, ircTarget = request.IrcTarget };
                        }
                    }
                    else
                    {
                        return new {msg="user null"};
                    }
                    //var subChatters = existingSubs.Where(x => request.Chatters.Contains(x)).ToArray();

                }



                //var twitchApi = new Kraken(_configuration, _dbContext);
                //await twitchApi.UpdateSubs();
                /*
                var grouping = (await _dbContext.JoinUserModels().Where(u => request.Chatters.Contains(u.UserIdentity.TwitchId))
                    .AsNoTracking()
                    .ToArrayAsync()).GroupBy(u => u.SubTier).ToArray();
                */

                //this endpoint is called frequently so can use this to check if there are tx's that need to be refunded
                await TxUtils.RefundUnclaimed(_dbContext);
                return ret;//changedRows / 2;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, "");
                throw e;
            }
        }
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("tipcorn")]
        public async Task<ActionResult<TxReceipt[]>> Tipcorn([FromBody] TipRequest tipRequest)
        {
            if (!TxUtils.AreTransactionsEnabled(_configuration))
            {
                return StatusCode(420);
            }

            if (tipRequest == null) throw new ArgumentNullException();
            if (tipRequest.From == null) throw new ArgumentNullException();
            if (tipRequest.To == null) throw new ArgumentNullException();
            if (tipRequest.To == tipRequest.From) return StatusCode((int)HttpStatusCode.BadRequest);
            if (tipRequest.Amount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);

            try
            {
                UserLivestream liveStream = null;
                if (!string.IsNullOrEmpty(tipRequest.IrcTarget))
                {
                    //var liveStream = await _dbContext.UserLivestream.FirstOrDefaultAsync(x => x.IrcTarget == tipRequest.IrcTarget);
                    var streamQuery = await _dbContext.UserLivestream
                        .Join(_dbContext.UserIdentity, x => x.UserId, x => x.UserId, (stream, identity) => new
                        {
                            stream,
                            identity
                        }).FirstOrDefaultAsync(x => x.identity.TwitchId == tipRequest.IrcTarget);
                    if (streamQuery != null)
                    {
                        if (!streamQuery.stream.EnableTransactions)
                        {
                            return StatusCode(420);
                        }

                        liveStream = streamQuery.stream;
                    }

                }

                tipRequest.FromUser = this.GetCachedUser();
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

                            fromStats.Add(new ColumnValuePair(nameof(UserStat.AmountOfTipsSent), 1));
                            fromStats.Add(new ColumnValuePair(nameof(UserStat.TotalSentBitcornViaTips), amount));

                            sql.Append(TxUtils.ModifyNumbers(table, fromStats, '+', pk, from.UserId));

                            var toStats = new List<ColumnValuePair>();
                            toStats.Add(new ColumnValuePair(nameof(UserStat.AmountOfTipsReceived), 1));
                            toStats.Add(new ColumnValuePair(nameof(UserStat.TotalReceivedBitcornTips), amount));

                            sql.Append(TxUtils.ModifyNumbers(table, toStats, '+', pk, to.UserId));

                            sql.Append(TxUtils.UpdateNumberIfTop(table, nameof(UserStat.LargestSentBitcornTip), amount, pk, from.UserId));
                            sql.Append(TxUtils.UpdateNumberIfTop(table, nameof(UserStat.LargestReceivedBitcornTip), amount, pk, to.UserId));
                            if (!string.IsNullOrEmpty(tipRequest.IrcTarget))
                            {
                                var ircTx = new IrcTarget();
                                ircTx.IrcChannel = tipRequest.IrcTarget;
                                ircTx.TxGroupId = transactions[0].Tx.TxGroupId;
                                _dbContext.IrcTarget.Add(ircTx);
                                var liveStreamTable = nameof(UserLivestream);
                                var livestreamKey = nameof(UserLivestream.UserId);
                                //var stream = await _dbContext.UserLivestream.AsNoTracking().FirstOrDefaultAsync(x => x.IrcTarget == tipRequest.IrcTarget);
                                //if (stream != null)
                                if (liveStream != null)
                                {
                                    sql.AppendLine(
                                        TxUtils.ModifyNumber(liveStreamTable, nameof(UserLivestream.AmountOfTipsSent), 1, '+', livestreamKey, liveStream.UserId));

                                    sql.AppendLine(
                                       TxUtils.ModifyNumber(liveStreamTable, nameof(UserLivestream.TotalSentBitcornViaTips), processInfo.TotalAmount, '+', livestreamKey, liveStream.UserId));
                                }
                            }
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
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(tipRequest));
                throw e;
            }

        }
    }
}
