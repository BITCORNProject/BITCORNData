using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils.Tx
{
    public static class TxUtils
    {

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
            if (!platformIds.Contains(fromPlatformId))
            {
                platformIds.Add(fromPlatformId);
            }
            var fromIdentity = await BitcornUtils.GetUserIdentityForPlatform(fromPlatformId,dbContext);
            if (fromIdentity == null)
                return null;

            var fromUser = await dbContext.User.FirstOrDefaultAsync(u=>u.UserId==fromIdentity.UserId);
      
            foreach (var to in req.To)
            {
                var toPlatformId = BitcornUtils.GetPlatformId(to);
                if (!platformIds.Contains(toPlatformId))
                {
                    platformIds.Add(toPlatformId);
                }
            }
            
            var identities = await BitcornUtils.GetUserIdentitiesForPlatform(platformIds.ToArray(),dbContext);
            var idKeys = identities.Select(u=>u.UserId).ToHashSet();
            var users = await dbContext.User.Where(u=>idKeys.Contains(u.UserId)).ToArrayAsync();
            var output = new TxReceipt[identities.Length];
            for (int i = 0; i < users.Length; i++)
            {
                var receipt = new TxReceipt();
                receipt.From = fromUser;
                receipt.To = users[i];
                if (fromUser != null && users[i] != null)
                {
                    receipt.Tx = MoveCorn(fromUser.UserWallet, users[i].UserWallet, req.Amount, req.Platform);
                }
                output[i] = receipt;
            }
            return output;
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
        public static CornTx MoveCorn(UserWallet from, UserWallet to, decimal amount,string platform)
        {
            if (from.Balance >= amount)
            {
                from.Balance -= amount;
                to.Balance += amount;

                var tx = new CornTx();
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
