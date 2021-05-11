using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Controllers;
using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Utils.Tx
{
    public static class TxUtils
    {
        public const int BitcornHubPK = 196;
        public static async Task AppendTxs(TxReceipt[] transactions, BitcornContext dbContext, string[] appendColumns)
        {
            if (appendColumns != null && appendColumns.Length > 0)
            {
                List<int> participants = new List<int>(transactions.Length + 1);
                if (transactions[0].From != null)
                {
                    participants.Add(transactions[0].From.UserId);
                }
                for (int i = 0; i < transactions.Length; i++)
                {
                    if (transactions[i].To != null)
                    {
                        participants.Add(transactions[i].To.UserId);
                    }
                }
                var columns = await UserReflection.GetColumns(dbContext, appendColumns, participants.ToArray());

                foreach (var tx in transactions)
                {
                    if (tx.From != null)
                    {
                        var fromColumns = columns[participants[0]];
                        foreach (var from in fromColumns)
                        {
                            tx.From.Add(from.Key, from.Value);
                        }
                    }
                    if (tx.To != null)
                    {
                        foreach (var to in columns[tx.To.UserId])
                        {
                            tx.To.Add(to.Key, to.Value);
                        }
                    }
                }
            }
        }

        public static async Task RefundUnclaimed(BitcornContext dbContext, int addExpirationMinutes = 60)
        {
            var now = DateTime.Now;
            var txs = await dbContext.UnclaimedTx.Where(t => t.Expiration.AddMinutes(addExpirationMinutes) < now && !t.Claimed && !t.Refunded)
                .Join(dbContext.UserWallet, tx => tx.SenderUserId, user => user.UserId, (tx, wallet) => new { Tx = tx, Wallet = wallet })
                .ToArrayAsync();
            if (txs.Length == 0)
                return;
            var pk = nameof(UserWallet.UserId);
            var sql = new StringBuilder();
            foreach (var entry in txs)
            {
                entry.Tx.Refunded = true;

                sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), entry.Tx.Amount, '+', pk, entry.Wallet.UserId));
            }
            await DbOperations.ExecuteSqlRawAsync(dbContext, sql.ToString());
            await dbContext.SaveAsync();
        }

        public static async Task<int> TryClaimTx(PlatformId platformId, User user, BitcornContext dbContext)
        {
            if (user == null)
            {
                user = await BitcornUtils.GetUserForPlatform(platformId, dbContext).FirstOrDefaultAsync();
            }
            if (user == null)
                return 0;

            bool lockApplied = false;
            try
            {
                if (UserLockCollection.Lock(user))
                {
                    lockApplied = true;
                }
                else
                {
                    return 0;
                }

                var now = DateTime.Now;
                var txs = await dbContext.UnclaimedTx.Where(u => u.Platform == platformId.Platform
                    && u.ReceiverPlatformId == platformId.Id
                    && !u.Claimed && !u.Refunded
                    && u.Expiration > now).ToArrayAsync();

                var sql = new StringBuilder();
                var pk = nameof(UserWallet.UserId);

                foreach (var tx in txs)
                {

                    tx.Claimed = true;

                    var log = new CornTx();
                    log.Amount = tx.Amount;
                    log.Platform = tx.Platform;
                    log.ReceiverId = user.UserId;
                    log.SenderId = tx.SenderUserId;
                    log.Timestamp = tx.Timestamp;
                    log.TxGroupId = Guid.NewGuid().ToString();
                    log.TxType = tx.TxType;
                    log.UnclaimedTx.Add(tx);
                    dbContext.CornTx.Add(log);

                    sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), tx.Amount, '+', pk, user.UserId));
                    sql.Append(TxUtils.ModifyNumber(nameof(UserStat), nameof(UserStat.AmountOfTipsReceived), 1, '+', pk, user.UserId));
                    sql.Append(TxUtils.ModifyNumber(nameof(UserStat), nameof(UserStat.TotalReceivedBitcornTips), tx.Amount, '+', pk, user.UserId));
                    sql.Append(TxUtils.UpdateNumberIfTop(nameof(UserStat), nameof(UserStat.LargestReceivedBitcornTip), tx.Amount, pk, user.UserId));


                    sql.Append(TxUtils.ModifyNumber(nameof(UserStat), nameof(UserStat.AmountOfTipsSent), 1, '+', pk, tx.SenderUserId));
                    sql.Append(TxUtils.ModifyNumber(nameof(UserStat), nameof(UserStat.TotalSentBitcornViaTips), tx.Amount, '+', pk, tx.SenderUserId));
                    sql.Append(TxUtils.UpdateNumberIfTop(nameof(UserStat), nameof(UserStat.LargestSentBitcornTip), tx.Amount, pk, tx.SenderUserId));

                }
                if (txs.Length > 0)
                {
                    await DbOperations.ExecuteSqlRawAsync(dbContext, sql.ToString());
                    await dbContext.SaveAsync();
                }
                return txs.Length;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(dbContext, e, JsonConvert.SerializeObject(platformId));
                return 0;
            }
            finally
            {
                if (lockApplied)
                {
                    UserLockCollection.Release(user);
                }
            }
        }

        public static async Task<User> GetBitcornhub(BitcornContext dbContext)
        {
            return await dbContext.JoinUserModels().FirstOrDefaultAsync(u => u.UserId == BitcornHubPK);
        }

        public static async Task<bool> SendFromBitcornhub(User to, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var processInfo = await PrepareTransaction(await GetBitcornhub(dbContext), to, amount, platform, txType, dbContext);
            return await processInfo.ExecuteTransaction(dbContext);
        }
        public static async Task<TxReceipt> SendFromBitcornhubGetReceipt(User to, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var processInfo = await PrepareTransaction(await GetBitcornhub(dbContext), to, amount, platform, txType, dbContext);
            if (await processInfo.ExecuteTransaction(dbContext))
            {

                return processInfo.Transactions[0];
            }
            return null;
        }
        public static async Task<TxReceipt> SendToBitcornhub(User from, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var processInfo = await PrepareTransaction(from, await GetBitcornhub(dbContext), amount, platform, txType, dbContext);
            if (await processInfo.ExecuteTransaction(dbContext))
            {

                return processInfo.Transactions[0];
            }
            return null;
        }

        public static async Task<bool> ExecuteTransaction(this TxProcessInfo processInfo, BitcornContext dbContext)
        {
            var sql = new StringBuilder();
            if (processInfo.WriteTransactionOutput(sql))
            {
                var rows = await DbOperations.ExecuteSqlRawAsync(dbContext, sql.ToString());
                if (rows > 0)
                {
                    await dbContext.SaveAsync();
                    try
                    {
                        await OnPostTransaction(processInfo, dbContext);
                    }
                    catch (Exception ex)
                    {
                        await BITCORNLogger.LogError(dbContext, ex, "failed to handle tx callback");
                    }
                }
                return rows > 0;
            }
            return false;
        }

        public static async Task OnPostTransaction(TxProcessInfo processInfo, BitcornContext dbContext)
        {
            if (processInfo.Transactions != null)
            {
                decimal subtract = 0;
                int txCount = 0;
                var balanceUpdates = new List<object>();
                foreach (var txInfo in processInfo.Transactions)
                {
                    if (txInfo.To != null && txInfo.To.User != null && txInfo.Tx != null)
                    {
                        txCount++;
                        if (txInfo.To.User.IsSocketConnected)
                        {
                            var newBalance = txInfo.To.User.UserWallet.Balance + txInfo.Tx.Amount;
                            balanceUpdates.Add(new
                            {
                                auth0Id = txInfo.To.User.UserIdentity.Auth0Id,
                                balance = newBalance
                            });
                        }
                        subtract += txInfo.Tx.Amount.Value;
                    }
                }

                if (txCount > 0)
                {
                    if (processInfo.From != null && processInfo.From.IsSocketConnected)
                    {
                        balanceUpdates.Add(new
                        {
                            auth0Id = processInfo.From.UserIdentity.Auth0Id,
                            balance = processInfo.From.UserWallet.Balance - subtract
                        });
                    }
                }

                if (balanceUpdates.Count > 0)
                {
                    await WebSocketsController.TryBroadcastToBitcornfarms(dbContext, "update-balances", balanceUpdates);
                }
            }
        }

        public static async Task<TxProcessInfo> PrepareTransaction(User from, User to, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var request = new TxRequest(from, amount, platform, txType, $"userid|{to.UserId}");
            return await ProcessRequest(request, dbContext);
        }
        public static async Task<bool> SendFrom(User from, User to, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var processInfo = await PrepareTransaction(from, to, amount, platform, txType, dbContext);
            return await processInfo.ExecuteTransaction(dbContext);
        }

        public static async Task<TxReceipt> SendFromGetReceipt(User from, User to, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var processInfo = await PrepareTransaction(from, to, amount, platform, txType, dbContext);
            if ((await processInfo.ExecuteTransaction(dbContext)))
            {
                return processInfo.Transactions[0];
            }

            return null;
        }


        public static bool AreTransactionsEnabled(IConfiguration conf)
        {
            try
            {
                var value = conf["Config:TxEnabled"];
                return bool.Parse(value);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<(SignedTx, TxReceipt)?> CreateSignedTx(BitcornContext dbContext, User from, decimal amount, string platform, string txType, int? gameId = null)
        {
            var bitcornhub = await GetBitcornhub(dbContext);
            var result = await SendFromGetReceipt(from, bitcornhub, amount, platform, txType, dbContext);
            if (result != null)
            {
                var tx = new SignedTx();
                tx.Id = Guid.NewGuid().ToString();
                tx.Amount = amount;
                tx.InCornTxId = result.TxId.Value;
                tx.SenderUserId = from.UserId;
                tx.Timestamp = DateTime.Now;
                tx.TxType = txType;
                tx.GameInstanceId = gameId;
                dbContext.SignedTx.Add(tx);
                await dbContext.SaveAsync();
                return (tx, result);
            }

            return null;
        }

        static HashSet<string> _SignedTxLock = new HashSet<string>();

        public static async Task<(SignedTx, TxReceipt)?> ClaimSignedTx(BitcornContext dbContext, User to, string key, GameInstance game = null)
        {
            lock (_SignedTxLock)
            {
                if (_SignedTxLock.Contains(key)) return null;
                _SignedTxLock.Add(key);
            }

            try
            {
                SignedTx signed = null;
                if (game == null)
                {
                    signed = await dbContext.SignedTx.FirstOrDefaultAsync(x => x.Id == key);
                    if (signed.GameInstanceId != null) throw new NotImplementedException();
                }
                else
                {
                    signed = await dbContext.SignedTx.FirstOrDefaultAsync(x => x.Id == key && game.GameId == x.GameInstanceId);
                }

                if (signed != null && signed.OutCornTxId == null)
                {
                    var srcTx = await dbContext.CornTx.FirstOrDefaultAsync(x => x.CornTxId == signed.InCornTxId);
                    if (srcTx != null)
                    {
                        if (srcTx.ReceiverId == BitcornHubPK)
                        {
                            var amount = srcTx.Amount.Value;
                            int? hostTx = null;
                            if (game != null)
                            {
                                var host = await dbContext.JoinUserModels().FirstOrDefaultAsync(x => x.UserId == game.HostId);
                                var hostReward = amount * 0.01m;
                                amount -= hostReward;
                                var hostReceipt = await SendFromBitcornhubGetReceipt(host, hostReward, srcTx.Platform, srcTx.TxType, dbContext);
                                if (hostReceipt != null)
                                {
                                    hostTx = hostReceipt.TxId;
                                }
                            }

                            var receipt = await SendFromBitcornhubGetReceipt(to, amount, srcTx.Platform, srcTx.TxType, dbContext);
                            if (receipt != null)
                            {
                                var cmd = $" update [{nameof(SignedTx)}] set [{nameof(SignedTx.OutCornTxId)}] = {receipt.TxId.Value} where [{nameof(SignedTx.Id)}] = '{signed.Id}' ";
                                if (hostTx != null)
                                {
                                    cmd += $" update [{nameof(SignedTx)}] set [{nameof(SignedTx.OutCornTxId2)}] = {hostTx.Value} where [{nameof(SignedTx.Id)}] = '{signed.Id}' ";


                                }
                                dbContext.Database.ExecuteSqlRaw(cmd);
                                //signed.OutCornTxId = receipt.TxId;
                                return (signed, receipt);
                            }
                        }
                    }
                    //SendFromBitcornhub(to, signed.Amount, signed.);
                }
            }
            catch (Exception ex)
            {
                await BITCORNLogger.LogError(dbContext, ex, "");
            }
            finally
            {
                lock (_SignedTxLock)
                {
                    _SignedTxLock.Remove(key);
                }
            }

            return null;
        }

        public static async Task<bool> ShouldLockWallet(BitcornContext dbContext, User user, decimal add)
        {
            if (user.UserId != TxUtils.BitcornHubPK)
            {
                if (user.UserId == 5957 || user.UserId == 6051) return false;
                var nowDayAgo = DateTime.Now;
                nowDayAgo = nowDayAgo.AddHours(-24);
                var spent = await dbContext.CornTx.Where(x => x.SenderId == user.UserId && x.Timestamp > nowDayAgo).SumAsync(x => x.Amount);
                if (spent + add > 30_000_000)
                {
                    user.UserWallet.IsLocked = true;
                    var cmd = $"UPDATE [{nameof(UserWallet)}] set [{nameof(UserWallet.IsLocked)}] = 1 where [{nameof(UserWallet)}].{nameof(UserWallet.UserId)} = {user.UserId}";
                    dbContext.Database.ExecuteSqlRaw(cmd);
                    return true;
                }

            }

            return false;

        }

        /// <summary>
        /// method to prepare a transaction, calling this method will not move fund immediately 
        /// </summary>
        /// <param name="req">transaction request</param>
        /// <returns>transaction tracker output</returns>
        public static async Task<TxProcessInfo> ProcessRequest(ITxRequest req, BitcornContext dbContext)
        {
            if (req.Amount <= 0)
            {
                throw new ArgumentException("Amount");
            }
            //create tx process info that will be tracking this transaction
            var info = new TxProcessInfo();
            //create hashset of receivers
            var platformIds = new HashSet<PlatformId>();
            //set sender user
            info.From = req.FromUser;
            //array of recipient ids
            var toArray = req.To.ToArray();
            //calculate total amount of corn being sent
            var totalAmountRequired = toArray.Length * req.Amount;
            bool canExecuteAll = false;

            //check if sender has enough corn to execute all transactions
            if (info.From != null && info.From.UserWallet.Balance >= totalAmountRequired)
            {

                if (info.From.UserWallet.IsLocked != null && info.From.UserWallet.IsLocked == true)
                {
                    canExecuteAll = false;
                }
                else
                {
                    if (!(await ShouldLockWallet(dbContext, info.From, totalAmountRequired)))
                    {
                        canExecuteAll = true;
                    }
                }
            }

            //get platform ids of recipients
            foreach (var to in toArray)
            {
                var toPlatformId = BitcornUtils.GetPlatformId(to);
                if (!platformIds.Contains(toPlatformId))
                {
                    platformIds.Add(toPlatformId);
                }
            }
            var platformIdArray = platformIds.ToArray();
            //get recipients query
            var userQuery = BitcornUtils.GetUsersForPlatform(platformIdArray, dbContext).AsNoTracking();
            //convert into dictionary mapped by their platformid
            var users = await BitcornUtils.ToPlatformDictionary(platformIdArray, userQuery, dbContext);
            //create list for receipts
            var output = new List<TxReceipt>();
            //create group transaction id
            var txid = Guid.NewGuid().ToString();

            var sql = new StringBuilder();
            //get corn usdt price at this time
            var cornUsdtPrice = (await ProbitApi.GetCornPriceAsync(dbContext));
            foreach (var to in platformIdArray)
            {
                var receipt = new TxReceipt();
                //if sender is registered, assign it to the receipt
                if (info.From != null)
                {
                    receipt.From = new SelectableUser(info.From);
                }
                //if recipient is registered, assign it to the receipt
                if (users.TryGetValue(to.Id, out User user))
                {
                    receipt.To = new SelectableUser(user);
                    //if all transactions can be executed, attempt to validate this transaction
                    if (canExecuteAll)
                    {
                        //verifytx returns corntx if this transaction will be made
                        receipt.Tx = VerifyTx(info.From, user, cornUsdtPrice, req.Amount, req.Platform, req.TxType, txid);
                    }
                    //if this transaction will be made, log it into corntx table
                    if (receipt.Tx != null)
                    {
                        dbContext.CornTx.Add(receipt.Tx);
                    }
                }
                //add receipt to the output
                output.Add(receipt);
            }
            //set receipts to the transaction tracker
            info.Transactions = output.ToArray();
            return info;
        }
        public static string UpdateNumberIfTop(string table, string column, decimal value, string primaryKeyName, params int[] primaryKeyValues)
        {
            var sql = new StringBuilder();
            var valueString = value.ToString(CultureInfo.InvariantCulture);
            sql.Append($" UPDATE [{table}] SET {column} = {valueString} where {valueString} > {column} and [{table}].{primaryKeyName} ");
            if (primaryKeyValues.Length == 1)
            {
                sql.Append('=');
                sql.Append(primaryKeyValues[0]);
            }
            else
            {
                sql.Append("in");
                sql.Append('(');
                sql.Append(string.Join(',', primaryKeyValues));
                sql.Append(')');
            }
            sql.Append(' ');
            return sql.ToString();
        }
        public static string ModifyNumber(string table, string column, decimal value, char operation, string primaryKeyName, params int[] primaryKeyValues)
        {
            var list = new List<ColumnValuePair>() { new ColumnValuePair(column, value) };
            return ModifyNumbers(table, list, operation, primaryKeyName, primaryKeyValues);
        }

        public static string ModifyNumbers(string table, List<ColumnValuePair> setters, char operation, string primaryKeyName, params int[] primaryKeyValues)
        {
            StringBuilder sql = new StringBuilder();

            if (!char.Equals('+', operation) && !char.Equals('-', operation))
                throw new ArgumentException("invalid operation:" + operation);

            sql.Append($" UPDATE [{table}] SET ");
            for (int i = 0; i < setters.Count; i++)
            {

                var item = setters[i];
                object value = null;
                if (item.Value is decimal)
                {
                    value = ((decimal)item.Value).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    value = item.Value.ToString();
                }

                if (i != 0)
                {
                    sql.Append(',');
                }
                sql.Append($" {item.Column} = {item.Column} {operation} {value} ");

            }

            sql.Append($" where [{table}].{primaryKeyName}  ");
            if (primaryKeyValues.Length == 1)
            {
                sql.Append('=');
                sql.Append(primaryKeyValues[0]);
            }
            else
            {
                sql.Append("in");
                sql.Append('(');
                sql.Append(string.Join(',', primaryKeyValues));
                sql.Append(')');
            }

            sql.Append(' ');
            return sql.ToString();
        }

        /// <summary>
        /// used to determine if this transaction should be executed
        /// </summary>
        /// <param name="from">send corn from</param>
        /// <param name="to">send corn to </param>
        /// <param name="cornUsdt">usdt value at the at this time</param>
        /// <param name="amount">amount of corn sent</param>
        /// <param name="platform">which application is making this transaction</param>
        /// <param name="txType">information about this transaction</param>
        /// <param name="txId">group transaction id</param>
        /// <returns>receipt of transaction, returns null if transaction will not be made</returns>
        public static CornTx VerifyTx(User from, User to, decimal cornUsdt, decimal amount, string platform, string txType, string txId)
        {
            if (from.UserWallet.IsLocked != null && from.UserWallet.IsLocked.Value)
            {
                return null;
            }
            if (amount <= 0)
                return null;
            if (from == null || to == null)
                return null;
            if (from.IsBanned || to.IsBanned)
                return null;
            if (from.UserId == to.UserId)
                return null;
            if (from.UserWallet.Balance >= amount)
            {
                var tx = new CornTx();
                tx.TxType = txType;
                tx.TxGroupId = txId;
                tx.Amount = amount;
                tx.ReceiverId = to.UserId;
                tx.SenderId = from.UserId;
                tx.Timestamp = DateTime.Now;
                tx.Platform = platform;
                tx.UsdtPrice = cornUsdt;
                tx.TotalUsdtValue = tx.Amount * tx.UsdtPrice;
                return tx;
            }
            return null;
        }

        internal static async Task<decimal> GetSoldCorn24h(BitcornContext dbContext)
        {
            return await dbContext.CornPurchase.Where(x => x.CreatedAt > DateTime.Now.AddHours(-24) && x.CornTxId != null)
                .SumAsync(x => x.CornAmount);
        }
    }
}
