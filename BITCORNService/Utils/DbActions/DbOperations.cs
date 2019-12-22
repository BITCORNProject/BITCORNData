using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace BITCORNService.Utils.DbActions
{
    public static class DbOperations
    {
        public static async Task<UserIdentity[]> Auth0ManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return await dbContext.UserIdentity.Where(u => ids.Contains(u.Auth0Id)).ToArrayAsync();
        }

        public static async Task<UserIdentity[]> TwitchManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return await dbContext.UserIdentity.Where(u => ids.Contains(u.TwitchId)).ToArrayAsync();
        }

        public static async Task<UserIdentity[]> DiscordManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return await dbContext.UserIdentity.Where(u => ids.Contains(u.DiscordId)).ToArrayAsync();
        }

        public static async Task<UserIdentity[]> TwitterManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return await dbContext.UserIdentity.Where(u => ids.Contains(u.TwitterId)).ToArrayAsync();
        }

        public static async Task<UserIdentity[]> RedditManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return await dbContext.UserIdentity.Where(u => ids.Contains(u.RedditId)).ToArrayAsync();
        }
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
        public static async Task SaveAsync(this BitcornContext dbContext, IDbContextTransaction transaction = null)
        {

            //create execution strategy so the request can retry if it fails to connect to the database
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                try
                {
                    await dbContext.SaveChangesAsync();
                    transaction?.Commit();
                }
                catch (Exception e)
                {
                    transaction?.Rollback();
                    Log.Logger.Error(e.Message, e);
                    throw new Exception(e.Message, e.InnerException);
                }

            });

        }
    }
}
