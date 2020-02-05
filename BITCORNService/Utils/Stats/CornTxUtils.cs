using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Stats
{
    public class CornTxUtils
    {

        public static string GetUsername(ReceivedTx received)
        {
            switch (received.Platform)
            {
                case "twitch":
                    return received.TwitchUsername;

                case "discord":
                    return received.DiscordUsername;

                case "reddit":
                    return received.RedditUsername;

                case "twitter":
                    return received.TwitterUsername;

                default:
                    return "null";

            }
        }
        public static async Task<TxRecordOutput[]> ListSentTransactions(BitcornContext dbContext, int user, HashSet<string> transactions)
        {

            List<TxRecordOutput> records = new List<TxRecordOutput>();
            var sentQuery = dbContext.CornTx.Where(tx => tx.SenderId == user && transactions.Contains(tx.TxGroupId)).Join(dbContext.UserIdentity,
                (CornTx tx) => tx.ReceiverId, (UserIdentity user) => user.UserId, (CornTx tx, UserIdentity user) => new ReceivedTx
                {
                    BlockchainTxId = tx.BlockchainTxId,
                    Platform = tx.Platform,
                    Timestamp = tx.Timestamp,
                    CornTxId = tx.CornTxId,
                    Amount = tx.Amount,
                    TxType = tx.TxType,
                    TxGroupId = tx.TxGroupId,
                    CornAddy = tx.CornAddy,
                    TwitchUsername = user.TwitchUsername,
                    DiscordUsername = user.DiscordUsername,
                    TwitterUsername = user.TwitterUsername,
                    RedditUsername = user.RedditUsername

                });
            var sent = await sentQuery.ToArrayAsync();
            var sentGrouping = sent.GroupBy(data => data.TxGroupId);
            foreach (var tx in sentGrouping)
            {

                var output = new TxRecordOutput();

                foreach (var received in tx)
                {
                    output.Action = "sent";
                    output.BlockchainTxId = received.BlockchainTxId;
                    if (received.BlockchainTxId != null)
                    {
                        Console.WriteLine("not null:"+received.BlockchainTxId);
                    }
                    output.CornAddy = received.CornAddy;
                    output.Platform = received.Platform;
                    output.Time = received.Timestamp.Value;
                    output.Amount += received.Amount.Value;
                    output.TxType = received.TxType;
                    output.Recipients.Add(GetUsername(received));
                }

                records.Add(output);

            }
            return records.ToArray();
        }
        public static async Task<TxRecordOutput[]> ListReceivedTransactions(BitcornContext dbContext, int user, HashSet<string> transactions)
        {
            var receivedQuery =dbContext.CornTx.Where(tx => tx.ReceiverId == user && transactions.Contains(tx.TxGroupId))
                .Join(dbContext.UserIdentity,
                (CornTx tx) => tx.SenderId, (UserIdentity user) => user.UserId, (CornTx tx, UserIdentity user) => new ReceivedTx
                {
                    BlockchainTxId = tx.BlockchainTxId,
                    Platform = tx.Platform,
                    Timestamp = tx.Timestamp,
                    CornTxId = tx.CornTxId,
                    Amount = tx.Amount,
                    TxType = tx.TxType,
                    TxGroupId = tx.TxGroupId,
                    CornAddy = tx.CornAddy,
                    TwitchUsername = user.TwitchUsername,
                    DiscordUsername = user.DiscordUsername,
                    TwitterUsername = user.TwitterUsername,
                    RedditUsername = user.RedditUsername

                });

            var receivedData = await receivedQuery.ToArrayAsync();
            var records = new List<TxRecordOutput>();
            foreach (var received in receivedData)
            {
                var output = new TxRecordOutput();
                output.Action = "receive";
                output.BlockchainTxId = received.BlockchainTxId;
                if (received.BlockchainTxId != null)
                {
                    Console.WriteLine("not null:" + received.BlockchainTxId);
                }
                output.Platform = received.Platform;
                output.Time = received.Timestamp.Value;
                output.Amount = received.Amount.Value;
                output.TxType = received.TxType;
                output.CornAddy = received.CornAddy;
                output.Recipients.Add(GetUsername(received));
                records.Add(output);
            }
            return records.ToArray();
        }

        public static async Task<TxRecordOutput[]> ListTransactions(BitcornContext dbContext, int user, int offset, int limit)
        {
            var transactions = await CornTxUtils.GetFullTransactionIds(dbContext, user, offset, limit);
            List<TxRecordOutput> outputs = new List<TxRecordOutput>();
            outputs.AddRange(await ListSentTransactions(dbContext, user, transactions));
            outputs.AddRange(await ListReceivedTransactions(dbContext,user,transactions));
            outputs.Sort((a,b)=>b.Time.CompareTo(a.Time));
            return outputs.ToArray();
        }
        public static async Task<HashSet<string>> GetFullTransactionIds(BitcornContext dbContext, int user,int offset = 0,int limit = 100)
        {
            HashSet<string> transactions = new HashSet<string>();
            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                var sql = new StringBuilder($"SELECT {nameof(CornTx.TxGroupId)}, {nameof(CornTx.CornTxId)}, {nameof(CornTx.ReceiverId)}, {nameof(CornTx.SenderId)} FROM (");
                sql.Append($"SELECT {nameof(CornTx.TxGroupId)}, {nameof(CornTx.CornTxId)}, {nameof(CornTx.ReceiverId)}, {nameof(CornTx.SenderId)},");
                sql.Append($"Row_number() OVER(PARTITION BY {nameof(CornTx.TxGroupId)} ORDER BY {nameof(CornTx.CornTxId)}) rn FROM {nameof(CornTx)}) t");
                sql.Append($" WHERE rn = 1 and ({nameof(CornTx.ReceiverId)} = {user} or {nameof(CornTx.SenderId)} = {user}) ");
                sql.Append($"order by {nameof(CornTx.CornTxId)} desc ");
                sql.Append($" offset {offset} rows");
                sql.Append($" fetch next {limit} rows only");

                command.CommandText = sql.ToString();
                command.CommandType = CommandType.Text;

                dbContext.Database.OpenConnection();
                using (var result = await command.ExecuteReaderAsync())
                {

                    while (result.Read())
                    {
                        transactions.Add(result.GetString(0));
                    }
                }
                dbContext.Database.CloseConnection();

            }


            return transactions;

        }
    }
}
