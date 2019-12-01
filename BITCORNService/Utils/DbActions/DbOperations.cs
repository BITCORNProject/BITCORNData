using System;
using System.Data;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils.DbActions
{
    public static class DbOperations
    {
        public static async Task<UserIdentity> Auth0Async(this BitcornContext dbContext, string auth0Id)
        {
            return await dbContext.UserIdentity.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);
        }

        public static async Task<UserIdentity> TwitchAsync(this BitcornContext dbContext, string twitchId)
        {
            return await dbContext.UserIdentity.FirstOrDefaultAsync(u => u.TwitchId == twitchId);
        }
        public static async Task<UserIdentity> TwitterAsync(this BitcornContext dbContext, string twitterId)
        {
            return await dbContext.UserIdentity.FirstOrDefaultAsync(u => u.TwitterId == twitterId);
        }

        public static async Task<UserIdentity> DiscordAsync(this BitcornContext dbContext, string discordId)
        {
            return await dbContext.UserIdentity.FirstOrDefaultAsync(u => u.DiscordId == discordId);
        }

        public static async Task<UserIdentity> RedditAsync(this BitcornContext dbContext, string redditId)
        {
            return await dbContext.UserIdentity.FirstOrDefaultAsync(u => u.RedditId == redditId);
        }

        public static async Task<UserWallet> WalletByAddress(this BitcornContext dbContext, string address)
        {
            return await dbContext.UserWallet.FirstOrDefaultAsync(w => w.CornAddy == address);
        }

        public static async Task<bool> IsBlockchainTransactionLogged(this BitcornContext dbContext, string txId)
        {
            return await dbContext.CornTx.AnyAsync(w => w.BlockchainTxId == txId);
        }

        public static async Task SaveAsync(this BitcornContext dbContext)
        {

            //create execution strategy so the request can retry if it fails to connect to the database
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = dbContext.Database.BeginTransaction(IsolationLevel.RepeatableRead))
                {
                    try
                    {
                        await dbContext.SaveChangesAsync();
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        //TODO dbContext.Logger.LogError(e, e.Message);
                        throw new Exception(e.Message, e.InnerException);
                    }
                }
            });

        }
    }
}
