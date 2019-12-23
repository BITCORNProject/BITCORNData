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
        public static async Task ExecuteRainTxs(IEnumerable<TxUser> txUsers, BitcornContext dbContext)
        {
            foreach (var txUser in txUsers)
            {
                var userIdentity = await dbContext.TwitchAsync(txUser.Id);
                var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);
               // await UpdateStats.RainedOn(userIdentity.UserId, txUser.Amount);
                if (userWallet != null) userWallet.Balance += txUser.Amount;
            }
            await dbContext.SaveAsync();
        }
        public static async Task<TxReceipt[]> ProcessRequest(ITxRequest req, BitcornContext dbContext)
        {
           // Dictionary<string, User> loadedUsers = new Dictionary<string, User>();
            HashSet<PlatformId> platformIds = new HashSet<PlatformId>();

            var fromPlatformId = BitcornUtils.GetPlatformId(req.From);
         
            var fromIdentity = await BitcornUtils.GetUserIdentityForPlatform(fromPlatformId,dbContext);
            if (fromIdentity == null)
                return null;

            var fromUser = await dbContext.User.FirstOrDefaultAsync(u=>u.UserId==fromIdentity.UserId);
            var fromUserWallet = await dbContext.UserWallet.FirstOrDefaultAsync(w=>w.UserId==fromIdentity.UserId);
            
            var toArray = req.To.ToArray();
         
            decimal totalAmountRequired = toArray.Length * req.Amount;
            if (fromUserWallet.Balance < totalAmountRequired)
                return null;
            
            foreach (var to in toArray)
            {
                var toPlatformId = BitcornUtils.GetPlatformId(to);
                if (!platformIds.Contains(toPlatformId))
                {
                    platformIds.Add(toPlatformId);
                }
            }
            
            var identities = await BitcornUtils.GetUserIdentitiesForPlatform(platformIds.ToArray(),dbContext);
            var idKeys = identities.Select(u=>u.UserId).ToHashSet();
            var wallets = await dbContext.UserWallet.Where(w=>idKeys.Contains(w.UserId)).ToArrayAsync();
       
            var output = new List<TxReceipt>();
         
            foreach (var wallet in wallets)
            {
                var receipt = new TxReceipt();
                receipt.From = new SelectableUser(fromUser.UserId);
                receipt.To = new SelectableUser(wallet.UserId);
                receipt.Tx = MoveCorn(fromUserWallet, wallet, req.Amount, req.Platform,req.TxType);
            
                if (receipt.Tx != null)
                {
                    dbContext.CornTx.Add(receipt.Tx);
                }
                output.Add(receipt);
            }
           
            return output.ToArray();
        }
        
        public static async Task ExecuteTipTx(TxUser txUser, BitcornContext dbContext)
        {
                var userIdentity = await dbContext.TwitchAsync(txUser.Id);
                var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);
                
                if (userWallet != null) userWallet.Balance += txUser.Amount;

                await dbContext.SaveAsync();
                //await UpdateStats.Tipped(userIdentity.UserId, txUser.Amount);
        }

        public static async Task ExecuteDebitTxs(IEnumerable<WithdrawUser> withdrawUsers, BitcornContext dbContext)
        {
            foreach (var txUser in withdrawUsers)
            {
                var userIdentity = await dbContext.TwitchAsync(txUser.Id);
                var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);
               // await UpdateStats.Rain(userIdentity.UserId, txUser.Amount);
                if (userWallet != null) userWallet.Balance -= txUser.Amount;

            }
            await dbContext.SaveAsync();
        }
        public static CornTx MoveCorn(UserWallet from, UserWallet to, decimal amount,string platform,string txType)
        {
            if (from.Balance >= amount)
            {
                from.Balance -= amount;
                to.Balance += amount;

                var tx = new CornTx();
                tx.TxType = txType;
                tx.Amount = amount;
                tx.ReceiverId = to.UserId;
                tx.SenderId = from.UserId;
                tx.Timestamp = DateTime.Now;
                tx.Platform = platform;
                return tx;
            }
            return null;
        }
      
        
        public static async Task ExecuteDebitTx(WithdrawUser withdrawUser, BitcornContext dbContext)
        {
                var userIdentity = await dbContext.TwitchAsync(withdrawUser.Id);
                var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);

                if (userWallet != null) userWallet.Balance -= withdrawUser.Amount;

                await dbContext.SaveAsync();
                //await UpdateStats.Tipped(userIdentity.UserId, withdrawUser.Amount);
        }
    }
}
