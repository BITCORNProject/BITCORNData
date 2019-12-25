using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils.Tx
{
    public static class TxUtils
    {
        public static async Task AppendTxs(TxReceipt[] transactions, BitcornContext dbContext, string[] appendColumns)
        {
            if (appendColumns.Length > 0)
            {
                int[] participants = new int[transactions.Length + 1];
                participants[0] = transactions[0].From.UserId;
                for (int i = 0; i < transactions.Length; i++)
                {
                    participants[i + 1] = transactions[i].To.UserId;
                }
                var columns = await UserReflection.GetColumns(dbContext, appendColumns, participants);
                var fromColumns = columns[participants[0]];
                foreach (var tx in transactions)
                {
                    foreach (var from in fromColumns)
                    {
                        tx.From.Add(from.Key, from.Value);
                    }
                    foreach (var to in columns[tx.To.UserId])
                    {
                        tx.To.Add(to.Key, to.Value);
                    }
                }
            }
        }
      
        public static async Task RefundUnclaimed(BitcornContext dbContext)
        {
            var now = DateTime.Now;
            var txs = await dbContext.UnclaimedTx.Where(t => t.Expiration.AddHours(1) < now && !t.Claimed && !t.Refunded)
                .Join(dbContext.UserWallet,tx=>tx.SenderUserId,user=>user.UserId,(tx,wallet)=> new { Tx = tx, Wallet = wallet})
                .ToArrayAsync();

            foreach (var item in txs)
            {
                item.Tx.Refunded = true;
                item.Wallet.Balance += item.Tx.Amount;
            }

            await dbContext.SaveAsync();
        }
        public static async Task TryClaimTx(PlatformId platformId, User user, BitcornContext dbContext)
        {
            var now = DateTime.Now;
            var txs = await dbContext.UnclaimedTx.Where(u => u.Platform == platformId.Platform
                && u.ReceiverPlatformId == platformId.Id
                && !u.Claimed && !u.Refunded
                && u.Expiration > now).ToArrayAsync();
         
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

                user.UserWallet.Balance += tx.Amount;
            }

            await dbContext.SaveAsync();

        }
        public static async Task<TxProcessInfo> ProcessRequest(ITxRequest req, BitcornContext dbContext)
        {
            var info = new TxProcessInfo();
           // Dictionary<string, User> loadedUsers = new Dictionary<string, User>();
            HashSet<PlatformId> platformIds = new HashSet<PlatformId>();

            var fromPlatformId = BitcornUtils.GetPlatformId(req.From);
         
            var fromUser = await BitcornUtils.GetUserForPlatform(fromPlatformId,dbContext).FirstOrDefaultAsync();
            if (fromUser == null)
                return info;

            info.From = fromUser;
            if (fromUser.IsBanned)
                return info;
            
            var toArray = req.To.ToArray();
         
            decimal totalAmountRequired = toArray.Length * req.Amount;
            if (fromUser.UserWallet.Balance < totalAmountRequired)
                return null;
            
            foreach (var to in toArray)
            {
                var toPlatformId = BitcornUtils.GetPlatformId(to);
                if (!platformIds.Contains(toPlatformId))
                {
                    platformIds.Add(toPlatformId);
                }
            }
            
            var users = await BitcornUtils.GetUsersForPlatform(platformIds.ToArray(),dbContext).ToArrayAsync();
           
            var output = new List<TxReceipt>();
            string txid = Guid.NewGuid().ToString();
            foreach (var user in users)
            {
                var receipt = new TxReceipt();
                receipt.From = new SelectableUser(fromUser);
                receipt.To = new SelectableUser(user);
                receipt.Tx = MoveCorn(fromUser, user, req.Amount, req.Platform,req.TxType,txid);
            
                if (receipt.Tx != null)
                {
                    dbContext.CornTx.Add(receipt.Tx);
                }
                output.Add(receipt);
            }
           
            info.Transactions = output.ToArray();
            return info;
        }
       
        public static CornTx MoveCorn(User from, User to, decimal amount,string platform,string txType,string txId)
        {
            if (from.IsBanned || to.IsBanned)
                return null;
            if (from.UserWallet.Balance >= amount)
            {
                from.UserWallet.Balance -= amount;
                to.UserWallet.Balance += amount;

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
