using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
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
    }
}
