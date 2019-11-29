using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Stats;

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
                await UpdateStats.RainedOn(userIdentity.UserId, txUser.Amount);
                if (userWallet != null) userWallet.Balance += txUser.Amount;
            }
            await dbContext.SaveAsync();
        }

        public static async Task ExecuteTipTx(TxUser txUser, BitcornContext dbContext)
        {
                var userIdentity = await dbContext.TwitchAsync(txUser.Id);
                var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);
                
                if (userWallet != null) userWallet.Balance += txUser.Amount;

                await dbContext.SaveAsync();
                await UpdateStats.Tipped(userIdentity.UserId, txUser.Amount);
        }

        public static async Task ExecuteDebitTxs(IEnumerable<WithdrawUser> withdrawUsers, BitcornContext dbContext)
        {
            foreach (var txUser in withdrawUsers)
            {
                var userIdentity = await dbContext.TwitchAsync(txUser.Id);
                var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);
                await UpdateStats.Rain(userIdentity.UserId, txUser.Amount);
                if (userWallet != null) userWallet.Balance -= txUser.Amount;

            }
            await dbContext.SaveAsync();
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
