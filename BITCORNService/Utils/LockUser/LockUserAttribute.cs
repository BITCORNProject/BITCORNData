using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace BITCORNService.Utils
{
    public class LockUserAttribute : ActionFilterAttribute
    {
        public static HashSet<string> LockedUsers = new HashSet<string>();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Request.Headers.TryGetValue("platform", out StringValues platform); 
            context.HttpContext.Request.Headers.TryGetValue("id", out StringValues id);

            base.OnActionExecuting(context);
        }


    }
}
