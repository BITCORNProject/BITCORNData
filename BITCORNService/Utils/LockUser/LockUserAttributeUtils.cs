using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
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

        public static async Task<int> GetUserId(ActionExecutingContext context, BitcornContext dbContext)
        {
            var platformHeaders = LockUserAttributeUtils.GetPlatformHeaders(context);

            switch (platformHeaders.Platform)
            {
                case "auth0":
                    return await dbContext.Auth0Query(platformHeaders.Id).Select(u=>u.UserId).FirstOrDefaultAsync();
                  
                case "twitch":
                    return await dbContext.TwitchQuery(platformHeaders.Id).Select(u => u.UserId).FirstOrDefaultAsync();
                case "discord":
                    return await dbContext.DiscordQuery(platformHeaders.Id).Select(u => u.UserId).FirstOrDefaultAsync();
                case "twitter":
                    return await dbContext.TwitterQuery(platformHeaders.Id).Select(u => u.UserId).FirstOrDefaultAsync();
                case "reddit":
                    return await dbContext.RedditQuery(platformHeaders.Id).Select(u => u.UserId).FirstOrDefaultAsync();
                default:
                    return 0;
            }
        }
    }
}
