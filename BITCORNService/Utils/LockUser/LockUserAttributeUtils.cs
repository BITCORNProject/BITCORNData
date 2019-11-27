using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace BITCORNService.Utils.LockUser
{
    public static class LockUserAttributeUtils
    {
        public static PlatformHeaders GetPlatformHeaders(ActionExecutingContext context)
        {
            context.HttpContext.Request.Headers.TryGetValue("platform", out StringValues platform);
            context.HttpContext.Request.Headers.TryGetValue("id", out StringValues id);
            return new PlatformHeaders {Id = id, Platform = platform};
        }

        public static async Task<int> GetUserId(ActionExecutingContext context)
        {
            var platformHeaders = LockUserAttributeUtils.GetPlatformHeaders(context);
            using (var dbContext = new BitcornContext())
            {
                UserIdentity user;
                switch (platformHeaders.Platform)
                {
                    case "auth0":
                        user = await dbContext.Auth0Async(platformHeaders.Id);
                        return user.UserId;
                    case "twitch":
                        user = await dbContext.TwitchAsync(platformHeaders.Id);
                        return user.UserId;
                    case "discord":
                        user = await dbContext.DiscordAsync(platformHeaders.Id);
                        return user.UserId;
                    case "twitter":
                        user = await dbContext.TwitterAsync(platformHeaders.Id);
                        return user.UserId;
                    case "reddit":
                        user = await dbContext.RedditAsync(platformHeaders.Id);
                        return user.UserId;
                    default:
                        return 0;
                }
            }

        }
    }
}
