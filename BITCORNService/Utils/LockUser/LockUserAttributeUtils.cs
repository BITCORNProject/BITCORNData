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

        public static IQueryable<User> GetUserFromHeader(ActionExecutingContext context, BitcornContext dbContext)
        {
            var platformHeaders = LockUserAttributeUtils.GetPlatformHeaders(context);

            switch (platformHeaders.Platform)
            {
                case "auth0":
                    return dbContext.Auth0Query(platformHeaders.Id);
                  
                case "twitch":
                    return dbContext.TwitchQuery(platformHeaders.Id);
                case "discord":
                    return dbContext.DiscordQuery(platformHeaders.Id);
                case "twitter":
                    return dbContext.TwitterQuery(platformHeaders.Id);
                case "reddit":
                    return dbContext.RedditQuery(platformHeaders.Id);
                default:
                    return null;
            }
        }
    }
}
