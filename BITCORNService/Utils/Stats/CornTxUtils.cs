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
                case "BITCORNFarms":
                    return received.Username;
                default:
                    return received.Username;

            }
        }
        public static async Task<TxRecordOutput[]> ListSentTransactions(BitcornContext dbContext, int user, HashSet<string> transactions)
        {

            List<TxRecordOutput> records = new List<TxRecordOutput>();
            var sentQuery = dbContext.CornTx.Where(tx => tx.SenderId == user && transactions.Contains(tx.TxGroupId))
                //.Join(dbContext.User,(tx)=>tx.ReceiverId (u)=>u.UserId,(tx,u)=>new { tx,u})
                .Join(dbContext.UserIdentity,
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
                    RedditUsername = user.RedditUsername,
                    Auth0Id = user.Auth0Id,
                    Auth0Nickname = user.Auth0Nickname,

                    Username = user.Username,

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

                    output.CornAddy = received.CornAddy;
                    output.Platform = received.Platform;
                    output.Time = received.Timestamp.Value;
                    output.Amount += received.Amount.Value;
                    output.TxType = received.TxType;
                    output.Recipients.Add(GetUsername(received));
                    output.GroupId = received.TxGroupId;
                    output.RecipientIds.Add(received.Auth0Id);
                }

                records.Add(output);

            }
            return records.ToArray();
        }
        public static async Task<TxRecordOutput[]> ListReceivedTransactions(BitcornContext dbContext, int user, HashSet<string> transactions)
        {
            var receivedQuery = dbContext.CornTx.Where(tx => tx.ReceiverId == user && transactions.Contains(tx.TxGroupId))
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
                    RedditUsername = user.RedditUsername,
                    Auth0Id = user.Auth0Id,
                    Username = user.Username,
                    Auth0Nickname = user.Auth0Nickname
                });

            var receivedData = await receivedQuery.ToArrayAsync();
            var records = new List<TxRecordOutput>();
            foreach (var received in receivedData)
            {
                var output = new TxRecordOutput();
                output.Action = "receive";
                output.BlockchainTxId = received.BlockchainTxId;

                output.Platform = received.Platform;
                output.Time = received.Timestamp.Value;
                output.Amount = received.Amount.Value;
                output.TxType = received.TxType;
                output.CornAddy = received.CornAddy;
                output.GroupId = received.TxGroupId;
                output.Recipients.Add(GetUsername(received));
                output.RecipientIds.Add(received.Auth0Id);
                records.Add(output);
            }
            return records.ToArray();
        }

        public static async Task<TxRecordOutput[]> ListTransactions(BitcornContext dbContext, int user, int offset, int limit, string[] txTypes)
        {
            try
            {
                var transactions = await CornTxUtils.GetFullTransactionIds(dbContext, user, offset, limit, txTypes);
                List<TxRecordOutput> outputs = new List<TxRecordOutput>();
                outputs.AddRange(await ListSentTransactions(dbContext, user, transactions));
                outputs.AddRange(await ListReceivedTransactions(dbContext, user, transactions));

                outputs.Sort((a, b) => b.Time.CompareTo(a.Time));

                var allTransactions = outputs.Skip(offset).Take(limit).ToArray();
                var mappedTransactions = allTransactions.GroupBy(x => x.GroupId).ToDictionary(x => x.Key, X => X.ToList());
                var ircTransactions = await dbContext.IrcTransaction.Join(dbContext.UserIdentity, (ircTx) => ircTx.IrcChannel, (u) => u.TwitchId,
                    (tx, u) => new { tx, u }
                    ).Join(dbContext.UserLivestream, (select) => select.u.UserId, (stream) => stream.UserId, (select, stream) => new
                    {
                        tx = select.tx,
                        u = select.u,
                        stream = stream
                    })
                    .Where(x => x.stream.Public && transactions.Contains(x.tx.TxGroupId)).ToArrayAsync();

                for (int i = 0; i < ircTransactions.Length; i++)
                {
                    var ircTx = ircTransactions[i];
                    if (mappedTransactions.TryGetValue(ircTx.tx.TxGroupId, out var outputRecord))
                    {
                        if (outputRecord != null)
                        {
                            for (int j = 0; j < outputRecord.Count; j++)
                            {
                                outputRecord[j].Message = ircTx.tx.IrcMessage;
                                if (!string.IsNullOrEmpty(ircTx.u.TwitchUsername))
                                {
                                    outputRecord[j].Channel = ircTx.u.TwitchUsername;
                                }
                                //outputRecord[j].
                            }
                        }
                    }
                }

                return allTransactions;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<HashSet<string>> GetFullTransactionIds(BitcornContext dbContext, int user, int offset = 0, int limit = 100, string[] txTypes = null)
        {
            HashSet<string> transactions = new HashSet<string>();
            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                bool txTypesDefined = false;
                /*
                var supportedTxTypes = new string[]
                {
                    "$withdraw" ,
                    "$rain",
                    "$tipcorn",
                    "receive" ,
                    "app:order",
                    "faucet",
                    "channel-points",
                    "sub-event" ,
                    "bit-donation",
                    "corn-purchase",
                    "Wallet download",
                    "Registrations reward",
                    "Recruit registrations reward",
                    "Recruit social sync",
                     "Social sync",
                     "Referral bonus reward",
                     "Recruit bonus reward"

                };
                if (txTypes != null && txTypes.Length > 0)
                {
                    txTypes = txTypes.Where(tx =>
                    supportedTxTypes.Contains(tx)
                    ).ToArray();
                    txTypesDefined = txTypes.Length > 0;
                }
                */
                var sql = new StringBuilder($"(select distinct  {nameof(CornTx.TxGroupId)} from  ");
                sql.Append($" (select * from {nameof(CornTx)} where ({nameof(CornTx.ReceiverId)} = {user} or {nameof(CornTx.SenderId)} = {user}) ");
                //txTypesDefined = false;
                /*if (txTypesDefined)
                {
                    sql.Append($" and {nameof(CornTx.TxType)} in ( ");
                    //{string.Join(",", txTypes)}
                    for (int i = 0; i < txTypes.Length; i++)
                    {
                        sql.Append($"'{txTypes[i]}'");
                        if (i != txTypes.Length - 1)
                        {
                            sql.Append(',');
                        }
                    }
                    sql.Append(") ");
                }*/

                sql.Append(" ) t) ");

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
