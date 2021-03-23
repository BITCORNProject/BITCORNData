using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.Models;
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
                        SubTier = user.SubTier,
                        CreationTime = user.CreationTime,
                        Avatar = user.Avatar,
                        IsBanned = user.IsBanned,
                        UserWallet = wallet,
                        UserStat = userStat,
                        UserIdentity = identity,
                        MFA = user.MFA,
                        IsSocketConnected = user.IsSocketConnected
                    });
        }

        public static IQueryable<LivestreamQueryResponse> GetLivestreams(this BitcornContext dbContext, bool requireToken = true)
        {

            return dbContext.JoinUserModels().Join(dbContext.UserLivestream, (user) => user.UserId, (stream) => stream.UserId, (user, stream) => new LivestreamQueryResponse
            {
                User = user,
                Stream = stream
            }).Where(x => requireToken && !string.IsNullOrEmpty(x.User.UserIdentity.TwitchRefreshToken) || !requireToken);
            /*
            return (from identity in dbContext.UserIdentity
                         join user in dbContext.User on identity.UserId equals user.UserId
                         join stream in dbContext.UserLivestream on identity.UserId equals stream.UserId
                         select new LivestreamQueryResponse
                         {
                             UserId = user.UserId,

                             UserIdentity = identity,
                             Stream = stream
                         });*/
        }

        public static IQueryable<User> Auth0ManyQuery(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.Auth0Id));
        }

        public static IQueryable<User> TwitchManyQuery(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => !string.IsNullOrEmpty(u.UserIdentity.TwitchId) && ids.Contains(u.UserIdentity.TwitchId));
        }

        public static IQueryable<User> TwitchUsernameManyQuery(this BitcornContext dbContext, HashSet<string> ids)
        {
            ids = ids.Select(x => x.ToLower()).ToHashSet();

            return JoinUserModels(dbContext).Where(u => !string.IsNullOrEmpty(u.UserIdentity.TwitchUsername) && ids.Contains(u.UserIdentity.TwitchUsername.ToLower()));
        }

        public static IQueryable<User> DiscordManyQuery(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.DiscordId));
        }

        public static IQueryable<User> TwitterManyQuery(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.TwitterId));
        }

        public static IQueryable<User> RedditManyQuery(this BitcornContext dbContext, HashSet<string> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserIdentity.RedditId));
        }
        public static IQueryable<User> UserIdManyQuery(this BitcornContext dbContext, HashSet<int> ids)
        {
            return JoinUserModels(dbContext).Where(u => ids.Contains(u.UserId));
        }
        public static IQueryable<User> UserIdQuery(this BitcornContext dbContext, int userId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserId == userId);
        }
        public static IQueryable<User> Auth0Query(this BitcornContext dbContext, string auth0Id)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.Auth0Id == auth0Id);
        }

        public static IQueryable<User> TwitchQuery(this BitcornContext dbContext, string twitchId)
        {
            return JoinUserModels(dbContext).Where(u => !string.IsNullOrEmpty(u.UserIdentity.TwitchId) && u.UserIdentity.TwitchId == twitchId);
        }

        public static IQueryable<User> TwitchUsernameQuery(this BitcornContext dbContext, string twitchUsername)
        {
            twitchUsername = twitchUsername.ToLower();
            return JoinUserModels(dbContext).Where(u => !string.IsNullOrEmpty(u.UserIdentity.TwitchUsername) && u.UserIdentity.TwitchUsername.ToLower() == twitchUsername);
        }

        public static IQueryable<User> TwitterQuery(this BitcornContext dbContext, string twitterId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.TwitterId == twitterId);
        }

        public static IQueryable<User> DiscordQuery(this BitcornContext dbContext, string discordId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.DiscordId == discordId);
        }

        public static IQueryable<User> RedditQuery(this BitcornContext dbContext, string redditId)
        {
            return JoinUserModels(dbContext).Where(u => u.UserIdentity.RedditId == redditId);
        }
        public static async Task<User> Auth0Async(this BitcornContext dbContext, string auth0Id)
        {
            return await Auth0Query(dbContext, auth0Id).FirstOrDefaultAsync();
        }

        public static async Task<User> TwitchAsync(this BitcornContext dbContext, string twitchId)
        {
            return await TwitchQuery(dbContext, twitchId).FirstOrDefaultAsync();
        }
        public static async Task<User> TwitterAsync(this BitcornContext dbContext, string twitterId)
        {
            return await TwitterQuery(dbContext, twitterId).FirstOrDefaultAsync();
        }

        public static async Task<User> DiscordAsync(this BitcornContext dbContext, string discordId)
        {
            return await DiscordQuery(dbContext, discordId).FirstOrDefaultAsync();
        }

        public static async Task<User> RedditAsync(this BitcornContext dbContext, string redditId)
        {
            return await RedditQuery(dbContext, redditId).FirstOrDefaultAsync();

        }

        public static async Task<UserWallet> WalletByAddress(this BitcornContext dbContext, string address)
        {
            return await dbContext.UserWallet.FirstOrDefaultAsync(w => w.CornAddy == address);
        }

        public static async Task<bool> IsDepositRegistered(this BitcornContext dbContext, string txId)
        {
            return await dbContext.CornDeposit.AnyAsync(w => w.TxId == txId);
        }
        public const bool DB_WRITES_ENABLED = true;//true;
        public static async Task<int> ExecuteSqlRawAsync(BitcornContext dbContext, string sql)
        {
            if (DB_WRITES_ENABLED)
            {
                return await dbContext.Database.ExecuteSqlRawAsync(sql);
            }
            return 0;
        }
        public static async Task<int> SaveAsync(this BitcornContext dbContext, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
        {
            if (!DB_WRITES_ENABLED) return 0;

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
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = typeof(T);
            var property = type.GetProperty(ordering, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderBy", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }
        public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = typeof(T);
            var property = type.GetProperty(ordering, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderByDescending", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }
    }
}
