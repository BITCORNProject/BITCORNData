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
        public static IQueryable<User> JoinUserModels(this BitcornContext dbContext)
        {
            return (from identity in dbContext.UserIdentity
                    join wallet in dbContext.UserWallet on identity.UserId equals wallet.UserId
                    join userStat in dbContext.UserStat on identity.UserId equals userStat.UserId
                    join user in dbContext.User on identity.UserId equals user.UserId
                    select new User
                    {
                        UserId = user.UserId,
                        Level = user.Level,
                        Username = user.Username,
                        Avatar = user.Avatar,
                        IsBanned = user.IsBanned,
                        UserWallet = wallet,
                        UserStat = userStat,
                        UserIdentity = identity,

                    });
        }

        public static IQueryable<User> Auth0ManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.Auth0Id));
        }

        public static IQueryable<User> TwitchManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.TwitchId));
        }

        public static IQueryable<User> DiscordManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.DiscordId));
        }

        public static IQueryable<User> TwitterManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.TwitterId));
        }

        public static IQueryable<User> RedditManyAsync(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.RedditId));
        }
        public static IQueryable<User> Auth0Async(this BitcornContext dbContext, string auth0Id)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.Auth0Id == auth0Id);
        }

        public static IQueryable<User> TwitchAsync(this BitcornContext dbContext, string twitchId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.TwitchId == twitchId);
        }
        public static IQueryable<User> TwitterAsync(this BitcornContext dbContext, string twitterId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.TwitterId == twitterId);
        }

        public static IQueryable<User> DiscordAsync(this BitcornContext dbContext, string discordId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.DiscordId == discordId);
        }

        public static IQueryable<User> RedditAsync(this BitcornContext dbContext, string redditId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.RedditId == redditId);
        }

        public static async Task<UserWallet> WalletByAddress(this BitcornContext dbContext, string address)
        {
            return await dbContext.UserWallet.FirstOrDefaultAsync(w => w.CornAddy == address);
        }

        public static async Task<bool> IsDepositRegistered(this BitcornContext dbContext, string txId)
        {
            return await dbContext.CornDeposit.AnyAsync(w => w.TxId == txId);
        }
        public static async Task<int> SaveAsync(this BitcornContext dbContext, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
        {

            //create execution strategy so the request can retry if it fails to connect to the database
            var strategy = dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = dbContext.Database.BeginTransaction(isolationLevel))
                {
                    try
                    {
                        int count = await dbContext.SaveChangesAsync();
                        transaction?.Commit();
                        return count;
                    }
                    catch (Exception e)
                    {
                        transaction?.Rollback();
                        Log.Logger.Error(e.Message, e);
                        throw new Exception(e.Message, e.InnerException);
                    }
                }

            });

        }
    }
}
