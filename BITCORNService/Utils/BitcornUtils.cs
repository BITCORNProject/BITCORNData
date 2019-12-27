using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils
{
    public static class BitcornUtils
    {
        public static PlatformId GetPlatformId(string routeId)
        {
            var platformId = new PlatformId();
            var parts = routeId.Split('|');
            platformId.Platform = parts[0].ToLower();
            platformId.Id = parts[1].ToLower();
            if (parts[0] == "auth0")
            {
                platformId.Id = routeId;
            }

            return platformId;
        }

        public static IQueryable<User> GetUserForPlatform(PlatformId platformId, BitcornContext dbContext)
        {
            switch (platformId.Platform)
            {
                case "auth0":
                    return dbContext.Auth0Async(platformId.Id);
                case "twitch":
                    return dbContext.TwitchAsync(platformId.Id);
                case "discord":
                    return dbContext.DiscordAsync(platformId.Id);
                case "twitter":
                    return dbContext.TwitterAsync(platformId.Id);
                case "reddit":
                    return dbContext.RedditAsync(platformId.Id);
                default:
                    throw new Exception($"User {platformId.Platform}|{platformId.Id} could not be found");
            }
        }
        public static async Task<Dictionary<string,User>> ToPlatformDictionary(PlatformId[] platformId,BitcornContext dbContext)
        {
            var query = GetUsersForPlatform(platformId,dbContext);
            switch (platformId[0].Platform)
            {
                case "auth0":
                    return await query.ToDictionaryAsync(u=>u.UserIdentity.Auth0Id,u=>u);
                case "twitch":
                    return await query.ToDictionaryAsync(u => u.UserIdentity.TwitchId, u => u);
                case "discord":
                    return await query.ToDictionaryAsync(u => u.UserIdentity.DiscordId, u => u);
                case "twitter":
                    return await query.ToDictionaryAsync(u => u.UserIdentity.TwitterId, u => u);
                case "reddit":
                    return await query.ToDictionaryAsync(u => u.UserIdentity.RedditId, u => u);
                default:
                    throw new Exception($"Platform {platformId[0].Platform} could not be found");
            }
        }
        public static IQueryable<User> GetUsersForPlatform(PlatformId[] platformId, BitcornContext dbContext)
        {
            HashSet<string> ids = platformId.Select(p=>p.Id).ToHashSet();
            
            switch (platformId[0].Platform)
            {
                case "auth0":
                    return dbContext.Auth0ManyAsync(ids);
                case "twitch":
                    return dbContext.TwitchManyAsync(ids);
                case "discord":
                    return dbContext.DiscordManyAsync(ids);
                case "twitter":
                    return dbContext.TwitterManyAsync(ids);
                case "reddit":
                    return dbContext.RedditManyAsync(ids);
                default:
                    throw new Exception($"Platform {platformId[0].Platform} could not be found");
            }
        }
        public static FullUser GetFullUser(User user, UserIdentity userIdentity, UserWallet userWallet, UserStat userStats)
        {
            var fullUser = new FullUser();
            fullUser.Username = user.Username;
            fullUser.UserId = user.UserId;
            fullUser.Avatar = user.Avatar;
            fullUser.Level = user.Level;
            fullUser.UserId = userIdentity.UserId;
            fullUser.Auth0Id = userIdentity.Auth0Id;
            fullUser.Auth0Nickname = userIdentity.Auth0Nickname;
            fullUser.TwitchId = userIdentity.TwitchId;
            fullUser.TwitchUsername = userIdentity.TwitchUsername;
            fullUser.DiscordId = userIdentity.DiscordId;
            fullUser.TwitterId = userIdentity.TwitterId;
            fullUser.RedditId = userIdentity.RedditId;
            fullUser.WalletServer = userWallet.WalletServer;
            fullUser.CornAddy = userWallet.CornAddy;
            fullUser.Balance = userWallet.Balance;
            fullUser.EarnedIdle = userStats.EarnedIdle;
            fullUser.Tipped = userStats.Tipped;
            fullUser.TippedTotal = userStats.TippedTotal;
            fullUser.TopTipped = userStats.TopTipped;
            fullUser.Tip = userStats.Tip;
            fullUser.TipTotal = userStats.TipTotal;
            fullUser.TopTip = userStats.TopTip;
            fullUser.Rained = userStats.Rained;
            fullUser.RainTotal = userStats.RainTotal;
            fullUser.TopRain = userStats.TopRain;
            fullUser.RainedOn = userStats.RainedOn;
            fullUser.RainedOnTotal = userStats.RainedOnTotal;
            fullUser.RainTotal = userStats.TopRainedOn;

            //call for twitter username
            //call for discord username
            return fullUser;
        }
    }
}
