using System;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;

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

        public static async Task<UserIdentity> GetUserIdentityForPlatform(PlatformId platformId, BitcornContext dbContext)
        {
            switch (platformId.Platform)
            {
                case "auth0":
                    return await dbContext.Auth0Async(platformId.Id);
                case "twitch":
                    return await dbContext.TwitchAsync(platformId.Id);
                case "discord":
                    return await dbContext.DiscordAsync(platformId.Id);
                case "twitter":
                    return await dbContext.TwitterAsync(platformId.Id);
                case "reddit":
                    return await dbContext.RedditAsync(platformId.Id);
                default:
                    throw new Exception($"User {platformId.Platform}|{platformId.Id} could not be found");
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
