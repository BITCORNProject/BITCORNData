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
                            var ircTx = new IrcTransaction();
                            ircTx.IrcChannel = rainRequest.IrcTarget;
                            ircTx.TxGroupId = transactions.Where(x => x.Tx != null).FirstOrDefault().Tx.TxGroupId;
                            ircTx.IrcMessage = rainRequest.IrcMessage;
                            _dbContext.IrcTransaction.Add(ircTx);

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
                        //DbOperations.ExecuteSqlRawAsync(_dbContext, 
                        await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
                        await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                        await TxUtils.OnPostTransaction(processInfo, _dbContext);
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

                if (payout <= 0) continue;

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
                    var val = await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
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
                    var val = await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
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
                        return new { msg = "user null" };
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


        public class StreamActionRequest
        {
            public string[] Columns { get; set; }
            public string Platform { get; set; }
            public string From { get; set; }

            public string Type { get; set; }
            public string IrcMessage { get; set; }

            public string IrcTarget { get; set; }

        }
        /*
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("streamaction")]
        public async Task<ActionResult<object>> StreamAction([FromBody] StreamActionRequest streamRequest)
        {
            if (!TxUtils.AreTransactionsEnabled(_configuration))
            {
                return StatusCode(420);
            }
            try
            {
                TxReceipt receipt = null;

                UserLivestream liveStream = null;
                if (!string.IsNullOrEmpty(streamRequest.IrcTarget))
                {
                    //var liveStream = await _dbContext.UserLivestream.FirstOrDefaultAsync(x => x.IrcTarget == tipRequest.IrcTarget);
                    var streamQuery = await _dbContext.UserLivestream
                        .Join(_dbContext.UserIdentity, x => x.UserId, x => x.UserId, (stream, identity) => new
                        {
                            stream,
                            identity
                        }).FirstOrDefaultAsync(x => x.identity.TwitchId == streamRequest.IrcTarget);
                    if (streamQuery != null)
                    {
                        if (!streamQuery.stream.EnableTransactions)
                        {
                            return StatusCode(420);
                        }

                        liveStream = streamQuery.stream;


                        var fromUser = this.GetCachedUser();
                        var toUser = await _dbContext.JoinUserModels().FirstOrDefaultAsync(x => x.UserId == liveStream.UserId);
                        if (fromUser != null && !fromUser.IsBanned)
                        {
                            if (streamRequest.Type == "tts")
                            {
                                if (liveStream.EnableTts)
                                {

                                    var costAmount = liveStream.BitcornPerTtsCharacter * streamRequest.IrcMessage.Length;
                                    var streamAction = new UserStreamAction();
                                    if (liveStream.BitcornPerTtsCharacter > 0 && toUser.UserId != fromUser.UserId)
                                    {
                                        var tx = await TxUtils.SendFromGetReceipt(fromUser, toUser, costAmount, "twitch", "tts", _dbContext);
                                        if (tx != null && tx.TxId != null)
                                        {
                                            streamAction.TxId = tx.TxId;
                                            receipt = tx;
                                        }
                                    }


                                    streamAction.RecipientUserId = liveStream.UserId;
                                    streamAction.SenderUserId = fromUser.UserId;
                                    streamAction.Timestamp = DateTime.Now;
                                    streamAction.Type = "tts";
                                    streamAction.Content = streamRequest.IrcMessage;
                                    if (toUser.IsSocketConnected)
                                    {
                                        await LivestreamUtils.HandleTts(_dbContext, fromUser, toUser, streamAction, false);
                                    }

                                    _dbContext.UserStreamAction.Add(streamAction);

                                    await _dbContext.SaveAsync();
                                    if (receipt != null)
                                    {
                                        await TxUtils.AppendTxs(new TxReceipt[] { receipt }, _dbContext, streamRequest.Columns);
                                    }

                                    return new
                                    {
                                        ttsEnabled = true,
                                        success = true,
                                        receipt = receipt
                                    };
                                }
                                else
                                {

                                    return new
                                    {
                                        ttsEnabled = false,
                                        success = false,
                                        receipt = receipt
                                    };
                                }
                            }
                        }
                    }

                }

                return new
                {
                    success = false,
                    receipt = receipt
                };
            }
            catch (Exception ex)
            {
                await BITCORNLogger.LogError(_dbContext, ex, "");
                return StatusCode(500);
            }
        }
        */
        StatusCodeResult CheckRequest(TipRequest tipRequest, bool checkAmount = true)
        {
            if (!TxUtils.AreTransactionsEnabled(_configuration))
            {
                return StatusCode(420);
            }


            if (tipRequest == null) throw new ArgumentNullException();
            if (tipRequest.From == null) throw new ArgumentNullException();
            if (tipRequest.To == null) throw new ArgumentNullException();
            if (tipRequest.To == tipRequest.From) return StatusCode((int)HttpStatusCode.BadRequest);
            if (checkAmount)
                if (tipRequest.Amount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);
            return null;
        }

        public class TransactionsNotEnabledException : Exception { }
        async Task<UserLivestream> GetLivestream(TipRequest tipRequest)
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
                        throw new TransactionsNotEnabledException();
                        //return StatusCode(420);
                    }

                    liveStream = streamQuery.stream;
                }

            }

            return liveStream;
        }

        async Task<bool> ProcessUnclaimed(TxProcessInfo processInfo, TipRequest tipRequest)
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
                    await DbOperations.ExecuteSqlRawAsync(_dbContext, TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), tipRequest.Amount, '-', nameof(UserWallet.UserId), processInfo.From.UserId));
                    await _dbContext.SaveAsync();
                    return true;
                }
            }

            return false;
        }

        public class BuyCornRequest
        {
            public string Auth0Id { get; set; }
            public string PaymentId { get; set; }
            public string OrderId { get; set; }
            public string Fingerprint { get; set; }
            public string ReceiptNumber { get; set; }
            public decimal UsdAmount { get; set; }
            public decimal CornAmount { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Token { get; set; }
        }
        static HashSet<string> s_LockedPayments = new HashSet<string>();
        static HashSet<string> s_PurchaseTokens = new HashSet<string>();
        static bool TryLockPayment(string l)
        {
            lock (s_LockedPayments)
            {
                if (s_LockedPayments.Contains(l))
                {
                    return false;
                }

                s_LockedPayments.Add(l);
                return true;
            }

        }
        public class CompleteBuyCornRequest
        {
            public string Auth0Id { get; set; }
            public int CornPurchaseId { get; set; }
            public string PaymentId { get; set; }
            public string Token { get; set; }
        }

        public class CloseBuycornResponse
        {
            public bool Success { get; set; }
            public int? PurchaseCloseId { get; set; }
            public string PaymentId { get; set; }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("completebuycorn")]
        [Authorize(Policy = AuthScopes.BuyCorn)]
        public async Task<ActionResult<CloseBuycornResponse>> CloseBuycorn([FromBody] CompleteBuyCornRequest completeRequest)
        {
            //return StatusCode(200);
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            try
            {
                if (!string.IsNullOrEmpty(completeRequest.PaymentId) && !string.IsNullOrEmpty(completeRequest.Token))
                {
                    lock (s_PurchaseTokens)
                    {
                        if (!s_PurchaseTokens.Contains(completeRequest.Token)) return StatusCode(420);
                    }

                    if (!TryLockPayment(completeRequest.PaymentId))
                    {
                        return StatusCode(420);
                    }

                    try
                    {
                        var platformId = BitcornUtils.GetPlatformId(completeRequest.Auth0Id);
                        var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                        if (user != null)
                        {
                            var purchase = await _dbContext.CornPurchase.Where(x => x.PaymentId == completeRequest.PaymentId && x.UserId == user.UserId && x.CornPurchaseId == completeRequest.CornPurchaseId).FirstOrDefaultAsync();
                            if (purchase != null && purchase.CornTxId == null)
                            {
                                if (purchase.CornAmount <= 0 || purchase.UsdAmount <= 0)
                                {
                                    return StatusCode(400);
                                }
                                //var prices = await ProbitApi.GetPricesAsync();
                                //var (cornBtc, btcUsdt, cornPrice) = prices;
                                //var costDiff = Math.Abs(purchase.UsdAmount-(purchase.CornAmount*cornPrice));
                                //if (costDiff < 2)
                                {
                                    var amount = purchase.CornAmount;
                                    var taxAmount = 0.3m;
                                    if (purchase.UsdAmount >= 50)
                                    {
                                        taxAmount = 0.2m;
                                    }
                                    amount -= amount * taxAmount;

                                    var value = await TxUtils.SendFromBitcornhubGetReceipt(user, amount, "BITCORNFarms", "corn-purchase", _dbContext);
                                    if (value != null && value.Tx != null)
                                    {
                                        purchase.CornTxId = value.TxId.Value;
                                        await _dbContext.SaveAsync();
                                        return new CloseBuycornResponse
                                        {
                                            Success = true,

                                            PurchaseCloseId = purchase.CornPurchaseId,
                                            PaymentId = purchase.PaymentId
                                        };
                                    }
                                }
                            }
                        }
                        return new CloseBuycornResponse
                        {
                            Success = false,

                        };
                    }
                    catch (Exception ex)
                    {
                        await BITCORNLogger.LogError(_dbContext, ex, JsonConvert.SerializeObject(completeRequest));
                        return new CloseBuycornResponse
                        {
                            Success = false
                        };
                    }
                    finally
                    {
                        lock (s_LockedPayments)
                        {
                            s_LockedPayments.Remove(completeRequest.PaymentId);
                        }


                    }


                }
                return StatusCode(400);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                lock (s_PurchaseTokens)
                {
                    s_PurchaseTokens.Remove(completeRequest.Token);
                }
            }

        }

        public class CanBuyCornResponse
        {
            public bool HasFunds { get; set; }
            public bool Success { get; set; }
            public bool GlobalCooldown { get; set; }
            public bool Cooldown { get; set; }
            public string Token { get; set; }
        }

        public const decimal SELL_CORN_CAP_24H = 50_000_000;

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{authid}/canbuycorn/{amount}")]

        [Authorize(Policy = AuthScopes.BuyCorn)]
        public async Task<ActionResult<CanBuyCornResponse>> CanBuycorn([FromRoute] string authid, [FromRoute] long amount)
        {
            var buyAmount = amount;
            var platformId = BitcornUtils.GetPlatformId(authid);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned && user.IsAdmin())
            {

                var soldAmount24h = await TxUtils.GetSoldCorn24h(_dbContext);
                var sellCap = SELL_CORN_CAP_24H;
                if (soldAmount24h + buyAmount > sellCap || Math.Abs(sellCap - soldAmount24h) < 10_000_00)
                {
                    return new CanBuyCornResponse
                    {

                        HasFunds = true,
                        Success = false,
                        GlobalCooldown = true,
                        Cooldown = false
                    };
                }
                else
                {
                    var cooldown = await _dbContext.CornPurchase.Where(x => x.UserId == user.UserId && x.CreatedAt > DateTime.Now.AddMinutes(-1) && x.CornTxId != null).CountAsync();
                    if (cooldown <= 0 && buyAmount >= 1)
                    {
                        var bitcornhub = await TxUtils.GetBitcornhub(_dbContext);
                        if (bitcornhub.UserWallet.Balance > buyAmount)
                        {
                            string purchaseToken = string.Empty;
                            lock (s_PurchaseTokens)
                            {
                                purchaseToken = Guid.NewGuid().ToString();
                                s_PurchaseTokens.Add(purchaseToken);
                            }

                            return new CanBuyCornResponse
                            {
                                HasFunds = true,
                                Success = true,
                                GlobalCooldown = false,
                                Cooldown = false,
                                Token = purchaseToken
                            };
                        }
                        else
                        {
                            return new CanBuyCornResponse
                            {
                                HasFunds = false,
                                Success = false,
                                GlobalCooldown = false,
                                Cooldown = false
                            };
                        }
                    }
                    else
                    {
                        return new CanBuyCornResponse
                        {
                            HasFunds = true,
                            Success = false,
                            GlobalCooldown = false,
                            Cooldown = false
                        };
                    }
                }
            }
            else
            {
                return StatusCode(404);
            }

        }

        public class PrepBuyCornResponse
        {
            public bool Success { get; set; }
            public string PaymentId { get; set; }
            public int PurchaseCloseId { get; set; }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("prepbuycorn")]

        [Authorize(Policy = AuthScopes.BuyCorn)]
        public async Task<ActionResult<PrepBuyCornResponse>> Buycorn([FromBody] BuyCornRequest request)
        {

            //return StatusCode(200);
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();

            if (request.CornAmount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);
            if (request.UsdAmount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);
            if (!string.IsNullOrEmpty(request.PaymentId))
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return StatusCode(420);
                }

                lock (s_PurchaseTokens)
                {
                    if (!s_PurchaseTokens.Contains(request.Token))
                    {
                        return StatusCode(420);
                    }
                }

                if (!TryLockPayment(request.PaymentId))
                {
                    return StatusCode(420);
                }

                try
                {
                    var platformId = BitcornUtils.GetPlatformId(request.Auth0Id);
                    var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                    if (user != null)
                    {

                        var prices = await ProbitApi.GetPricesAsync(_dbContext);
                        var (cornBtc, btcUsdt, cornPrice) = prices;
                        var costDiff = 0.0m;
                        var expectedCost = cornPrice * request.CornAmount;

                        if (expectedCost > 0 && request.CornAmount > 0 && request.UsdAmount > 0)
                        {
                            if (expectedCost < request.UsdAmount)
                            {
                                costDiff = 1 - (expectedCost / request.UsdAmount);
                            }
                            else if (request.UsdAmount < expectedCost)
                            {
                                costDiff = 1 - (request.UsdAmount / expectedCost);
                            }
                            else
                            {
                                costDiff = 0;
                            }

                            var existingPurhcase = await _dbContext.CornPurchase.Where(x => x.OrderId == request.OrderId).FirstOrDefaultAsync();

                            //Math.Abs(request.UsdAmount - (request.CornAmount * cornPrice));
                            if (costDiff < 0.001m && existingPurhcase == null)
                            {
                                var purchase = new CornPurchase();
                                purchase.OrderId = request.OrderId;
                                purchase.PaymentId = request.PaymentId;
                                purchase.ReceiptNumber = request.ReceiptNumber;
                                purchase.UsdAmount = request.UsdAmount;
                                purchase.CornAmount = request.CornAmount;
                                purchase.Fingerprint = request.Fingerprint;
                                purchase.UserId = user.UserId;
                                purchase.CreatedAt = request.CreatedAt;
                                _dbContext.CornPurchase.Add(purchase);
                                int count = await _dbContext.SaveAsync();
                                return new PrepBuyCornResponse
                                {
                                    Success = count > 0,
                                    PurchaseCloseId = purchase.CornPurchaseId,
                                    PaymentId = request.PaymentId
                                };
                            }
                        }
                    }
                    return new PrepBuyCornResponse
                    {
                        Success = false,
                        PurchaseCloseId = -1//purchase.CornPurchaseId
                    };
                }
                catch (Exception ex)
                {
                    await BITCORNLogger.LogError(_dbContext, ex, JsonConvert.SerializeObject(request));
                    return new PrepBuyCornResponse
                    {
                        Success = false,
                        PurchaseCloseId = -1//purchase.CornPurchaseId

                    };
                }
                finally
                {
                    lock (s_LockedPayments)
                    {
                        s_LockedPayments.Remove(request.PaymentId);
                    }


                }


            }
            return StatusCode(400);
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("bitdonation")]
        public async Task<ActionResult<TxReceipt[]>> BitDonation([FromBody] BitDonationRequest tipRequest)
        {
            var status = CheckRequest(tipRequest, false);
            if (tipRequest.BitAmount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);
            if (status != null) return status;

            try
            {
                UserLivestream liveStream = null;
                try
                {
                    liveStream = await GetLivestream(tipRequest);
                }
                catch (TransactionsNotEnabledException _)
                {
                    return StatusCode(420);
                }

                tipRequest.FromUser = this.GetCachedUser();
                if (tipRequest.FromUser != null && !tipRequest.FromUser.IsBanned && liveStream.BitcornhubFunded)
                {
                    tipRequest.FromUser = await TxUtils.GetBitcornhub(_dbContext);
                }
                tipRequest.Amount = tipRequest.BitAmount * liveStream.BitcornPerBit;
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

                            if (!string.IsNullOrEmpty(tipRequest.IrcTarget))
                            {
                                var ircTx = new IrcTransaction();
                                ircTx.IrcChannel = tipRequest.IrcTarget;
                                ircTx.TxGroupId = transactions[0].Tx.TxGroupId;
                                ircTx.IrcMessage = tipRequest.IrcMessage;
                                _dbContext.IrcTransaction.Add(ircTx);

                            }

                            await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
                            await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                            await TxUtils.OnPostTransaction(processInfo, _dbContext);
                        }
                    }
                    else
                    {
                        await ProcessUnclaimed(processInfo, tipRequest);
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

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("subevent")]
        public async Task<ActionResult<TxReceipt[]>> SubEvent([FromBody] ChannelSubRequest tipRequest)
        {
            var status = CheckRequest(tipRequest, false);

            if (status != null) return status;

            try
            {
                UserLivestream liveStream = null;
                try
                {
                    liveStream = await GetLivestream(tipRequest);
                }
                catch (TransactionsNotEnabledException _)
                {
                    return StatusCode(420);
                }

                tipRequest.FromUser = this.GetCachedUser();
                if (tipRequest.FromUser != null && !tipRequest.FromUser.IsBanned && liveStream.BitcornhubFunded)
                {
                    tipRequest.FromUser = await TxUtils.GetBitcornhub(_dbContext);
                }
                if (tipRequest.SubTier == "1000")
                {
                    tipRequest.Amount = liveStream.Tier1SubReward;
                }

                else if (tipRequest.SubTier == "2000")
                {
                    tipRequest.Amount = liveStream.Tier2SubReward;
                }

                else if (tipRequest.SubTier == "3000")
                {
                    tipRequest.Amount = liveStream.Tier3SubReward;
                }
                else
                {
                    return StatusCode((int)HttpStatusCode.BadRequest);
                }
                if (tipRequest.Amount <= 0)
                {
                    return StatusCode((int)HttpStatusCode.BadRequest);
                }

                //tipRequest.Amount = tipRequest.UsdAmount * liveStream.BitcornPerDonation;
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

                            if (!string.IsNullOrEmpty(tipRequest.IrcTarget))
                            {
                                var ircTx = new IrcTransaction();
                                ircTx.IrcChannel = tipRequest.IrcTarget;
                                ircTx.TxGroupId = transactions[0].Tx.TxGroupId;
                                ircTx.IrcMessage = tipRequest.IrcMessage;
                                _dbContext.IrcTransaction.Add(ircTx);

                            }

                            await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
                            await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                            await TxUtils.OnPostTransaction(processInfo, _dbContext);
                        }
                    }
                    else
                    {
                        await ProcessUnclaimed(processInfo, tipRequest);
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

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("channelpoints")]
        public async Task<ActionResult<TxReceipt[]>> RedeemChannelpoints([FromBody] ChannelPointsRedemptionRequest tipRequest)
        {
            var status = CheckRequest(tipRequest, false);
            if (status != null) return status;

            try
            {
                UserLivestream liveStream = null;
                try
                {
                    liveStream = await GetLivestream(tipRequest);
                }
                catch (TransactionsNotEnabledException _)
                {
                    return StatusCode(420);
                }

                tipRequest.FromUser = this.GetCachedUser();
                if (tipRequest.FromUser != null && !tipRequest.FromUser.IsBanned && liveStream.BitcornhubFunded)
                {
                    tipRequest.FromUser = await TxUtils.GetBitcornhub(_dbContext);
                }
                tipRequest.Amount = tipRequest.ChannelPointAmount * liveStream.BitcornPerChannelpointsRedemption;
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

                            if (!string.IsNullOrEmpty(tipRequest.IrcTarget))
                            {
                                var ircTx = new IrcTransaction();
                                ircTx.IrcChannel = tipRequest.IrcTarget;
                                ircTx.TxGroupId = transactions[0].Tx.TxGroupId;
                                ircTx.IrcMessage = tipRequest.IrcMessage;
                                _dbContext.IrcTransaction.Add(ircTx);

                            }

                            await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
                            await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                            await TxUtils.OnPostTransaction(processInfo, _dbContext);
                        }
                    }
                    else
                    {
                        await ProcessUnclaimed(processInfo, tipRequest);
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

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("tipcorn")]
        public async Task<ActionResult<TxReceipt[]>> Tipcorn([FromBody] TipRequest tipRequest)
        {
            var status = CheckRequest(tipRequest);
            if (status != null) return status;

            try
            {
                UserLivestream liveStream = null;
                try
                {
                    liveStream = await GetLivestream(tipRequest);
                }
                catch (TransactionsNotEnabledException _)
                {
                    return StatusCode(420);
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
                                var ircTx = new IrcTransaction();
                                ircTx.IrcChannel = tipRequest.IrcTarget;
                                ircTx.TxGroupId = transactions[0].Tx.TxGroupId;
                                ircTx.IrcMessage = tipRequest.IrcMessage;
                                _dbContext.IrcTransaction.Add(ircTx);
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
                            await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
                            await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                            await TxUtils.OnPostTransaction(processInfo, _dbContext);
                        }
                    }
                    else
                    {
                        await ProcessUnclaimed(processInfo, tipRequest);
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
