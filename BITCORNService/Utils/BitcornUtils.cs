using System;
using System.Collections.Generic;
using System.Linq;
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
            return platformId;
        }

        public static async Task<UserIdentity> GetUserIdentityForPlatform(PlatformId platformId, BitcornContext dbcontext)
        {
            switch (platformId.Platform)
            {
                case "auth0":
                    return await dbcontext.Auth0Async(platformId.Id);
                case "twitch":
                    return await dbcontext.TwitchAsync(platformId.Id);
                case "discord":
                    return await dbcontext.DiscordAsync(platformId.Id);
                case "twitter":
                    return await dbcontext.TwitterAsync(platformId.Id);
                case "Reddit":
                    return await dbcontext.RedditAsync(platformId.Id);
                default:
                    throw new Exception($"User {platformId.Platform}|{platformId.Id} could not be found");
            }
        }
    }
}
