﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace BITCORNService.Utils
{
    public static class BitcornUtils
    {
        public static PlatformId GetPlatformId(string routeId)
        {
            var platformId = new PlatformId();
            var parts = routeId.Split('|');
            platformId.Platform = parts[0].ToLower();
            platformId.Id = parts[1];
            if (parts[0] == "auth0")
            {
                platformId.Id = routeId;
            }

            return platformId;
        }
        public static async Task<UserIdentity> GetUserIdentityForPlatform(PlatformId platformId, BitcornContext dbContext)
        {
            return await GetUserForPlatform(platformId,dbContext).Select(u=>u.UserIdentity).FirstOrDefaultAsync();
        }
        public static IQueryable<User> GetUserForPlatform(PlatformId platformId, BitcornContext dbContext)
        {
            switch (platformId.Platform)
            {
                case "auth0":
                    return dbContext.Auth0Query(platformId.Id);
                case "twitch":
                    return dbContext.TwitchQuery(platformId.Id);
                case "discord":
                    return dbContext.DiscordQuery(platformId.Id);
                case "twitter":
                    return dbContext.TwitterQuery(platformId.Id);
                case "reddit":
                    return dbContext.RedditQuery(platformId.Id);
                default:
                    throw new Exception($"User {platformId.Platform}|{platformId.Id} could not be found");
            }
        }

        public static async Task<Dictionary<string,User>> ToPlatformDictionary(PlatformId[] platformId,BitcornContext dbContext)
        {
            var query = GetUsersForPlatform(platformId,dbContext);
            return await ToPlatformDictionary(platformId,query,dbContext);
        }
        public static async Task<Dictionary<string, User>> ToPlatformDictionary(PlatformId[] platformId, IQueryable<User> query, BitcornContext dbContext)
        {
            switch (platformId[0].Platform)
            {
                case "auth0":
                    return await query.ToDictionaryAsync(u => u.UserIdentity.Auth0Id, u => u);
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
                    return dbContext.Auth0ManyQuery(ids);
                case "twitch":
                    return dbContext.TwitchManyQuery(ids);
                case "discord":
                    return dbContext.DiscordManyQuery(ids);
                case "twitter":
                    return dbContext.TwitterManyQuery(ids);
                case "reddit":
                    return dbContext.RedditManyQuery(ids);
                default:
                    throw new Exception($"Platform {platformId[0].Platform} could not be found");
            }
        }

        public static async Task TxTracking(IConfiguration config, object data)
        {
            var client = new RestClient(config["Config:TxTrackingEndpoint"]);
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(data);

            var cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await client.ExecuteTaskAsync(request, cancellationTokenSource.Token);
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }
        public static async Task<UserIdentity> DeleteIdForPlatform(UserIdentity userIdentity, PlatformId platformId, BitcornContext dbContext)
        {
            switch (platformId.Platform.ToLower())
            {
                case "auth0":
                    userIdentity.Auth0Id = null;
                    userIdentity.Auth0Nickname = null;
                    await dbContext.SaveAsync();
                    break;
                case "twitch":
                    userIdentity.TwitchId = null;
                    userIdentity.TwitchUsername = null;
                    await dbContext.SaveAsync();
                    break;
                case "discord":
                    userIdentity.DiscordId = null;
                    userIdentity.DiscordUsername = null;
                    await dbContext.SaveAsync();
                    break;
                case "twitter":
                    userIdentity.TwitterId = null;
                    userIdentity.TwitterUsername = null;
                    await dbContext.SaveAsync();
                    break;
                case "reddit":
                    userIdentity.RedditId = null;
                    await dbContext.SaveAsync();
                    break;
                default:
                    throw new Exception($"User {platformId.Platform}|{platformId.Id} could not be found");
            }

            return userIdentity;
        }
        public static string BuildSubtierUpdateString(List<Sub> subs, int tier)
        {
            var sb = new StringBuilder();
            var prefix = $"UPDATE [user] SET subtier = {tier} " +
                         "FROM [userIdentity] " +
                         "inner join [user]" +
                         "  on [useridentity].userid = [user].userid" +
                         " where [useridentity].twitchid in (";
            sb.Append(prefix);

            foreach (var sub in subs)
            {
                sb.Append($"{sub.TwitchId}, ");
            }

            sb.Length -= 2;
            sb.Append(")");
            return sb.ToString();
        }

        public static FullUser GetFullUser(User user, UserIdentity userIdentity, UserWallet userWallet, UserStat userStats)
        {
            var fullUser = new FullUser()
            {
                Username = user.Username,
                UserId = user.UserId,
                Avatar = user.Avatar,
                Level = user.Level,

                Auth0Id = userIdentity.Auth0Id,
                Auth0Nickname = userIdentity.Auth0Nickname,
                TwitchId = userIdentity.TwitchId,
                TwitchUsername = userIdentity.TwitchUsername,
                TwitterUsername = userIdentity.TwitterUsername,
                DiscordUsername = userIdentity.DiscordUsername,
                DiscordId = userIdentity.DiscordId,
                TwitterId = userIdentity.TwitterId,
                RedditId = userIdentity.RedditId,
                WalletServer = userWallet.WalletServer,
                CornAddy = userWallet.CornAddy,
                Balance = userWallet.Balance,
                EarnedIdle = userStats.EarnedIdle,
                AmountOfTipsReceived = userStats.AmountOfTipsReceived,
                TotalReceivedBitcornTips = userStats.TotalReceivedBitcornTips,
                LargestReceivedBitcornTip = userStats.LargestReceivedBitcornTip,
                AmountOfTipsSent = userStats.AmountOfTipsSent,
                TotalSentBitcornViaTips = userStats.TotalSentBitcornViaTips,
                LargestSentBitcornTip = userStats.LargestSentBitcornTip,
                AmountOfRainsSent = userStats.AmountOfRainsSent,
                TotalSentBitcornViaRains = userStats.TotalSentBitcornViaRains,
                LargestSentBitcornRain = userStats.LargestSentBitcornRain,
                AmountOfRainsReceived = userStats.AmountOfRainsReceived,
                TotalReceivedBitcornRains = userStats.TotalReceivedBitcornRains,
                SubTier = user.SubTier,
                LargestReceivedBitcornRain = userStats.LargestReceivedBitcornRain
            };
        
            //call for twitter username
            //call for discord username
            return fullUser;
        }
    }
}
