using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BITCORNService.Utils.Tx
{
    public static class TxUtils
    {
        public const int BitcornHubPK = 196;
        public static async Task AppendTxs(TxReceipt[] transactions, BitcornContext dbContext, string[] appendColumns)
        {
            if (appendColumns.Length > 0)
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
            await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
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
                    await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                    await dbContext.SaveAsync();
                }
                return txs.Length;
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(dbContext,e,JsonConvert.SerializeObject(platformId));
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
        
        public static async Task<bool> ExecuteTransaction(this TxProcessInfo processInfo,BitcornContext dbContext)
        {
            var sql = new StringBuilder();
            if (processInfo.WriteTransactionOutput(sql))
            {
                var rows = await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                return rows > 0;
            }
            return false;
        }

        public static async Task<TxProcessInfo> PrepareTransaction(User from, User to, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var request = new TxRequest(from, amount, platform, txType, $"userid|{to.UserId}");
            return await ProcessRequest(request,dbContext);
        }
        public static async Task<bool> SendFrom(User from, User to, decimal amount, string platform, string txType, BitcornContext dbContext)
        {
            var processInfo = await PrepareTransaction(from, to, amount, platform, txType, dbContext);
            return await processInfo.ExecuteTransaction(dbContext);
        }
        public static async Task<TxProcessInfo> ProcessRequest(ITxRequest req, BitcornContext dbContext)
        {
            if (req.Amount <= 0)
            {
                throw new ArgumentException("Amount");
            }

            var info = new TxProcessInfo();
            var platformIds = new HashSet<PlatformId>();
            
            info.From = req.FromUser;
            
            var toArray = req.To.ToArray();
         
            var totalAmountRequired = toArray.Length * req.Amount;
            bool canExecuteAll = false;
            if (info.From!= null && info.From.UserWallet.Balance >= totalAmountRequired)
                canExecuteAll = true;
            
            foreach (var to in toArray)
            {
                var toPlatformId = BitcornUtils.GetPlatformId(to);
                if (!platformIds.Contains(toPlatformId))
                {
                    platformIds.Add(toPlatformId);
                }
            }
            var platformIdArray = platformIds.ToArray();
            var userQuery = BitcornUtils.GetUsersForPlatform(platformIdArray,dbContext).AsNoTracking();
            var users = await BitcornUtils.ToPlatformDictionary(platformIdArray,userQuery,dbContext);
           
            var output = new List<TxReceipt>();
            var txid = Guid.NewGuid().ToString();
            var sql = new StringBuilder();
            foreach (var to in platformIdArray)
            {
                var receipt = new TxReceipt();
                if (info.From != null)
                {
                    receipt.From = new SelectableUser(info.From);
                }
                
                if (users.TryGetValue(to.Id, out User user))
                {
                    receipt.To = new SelectableUser(user);
                    if (canExecuteAll)
                    {
                        receipt.Tx = VerifyTx(info.From, user, req.Amount, req.Platform, req.TxType, txid);
                    }
                    if (receipt.Tx != null)
                    {
                        dbContext.CornTx.Add(receipt.Tx);
                    }
                }
                output.Add(receipt);
            }
            
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

        public static CornTx VerifyTx(User from, User to, decimal amount,string platform,string txType,string txId)
        {
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
                return tx;
            }
            return null;
        }
      
    }
}
