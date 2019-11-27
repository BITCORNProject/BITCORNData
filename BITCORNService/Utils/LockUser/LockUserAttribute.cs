using System;
using System.Collections.Generic;
using System.Linq;
<<<<<<< HEAD
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace BITCORNService.Utils
{
    public class LockUserAttribute : ActionFilterAttribute
    {
        public static HashSet<int> LockedUsers = new HashSet<int>();

        public override async void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = await LockUserAttributeUtils.GetUserId(context);

            if (userId == 0) throw new Exception("User not found");

            var userLocked = LockedUsers.Contains(userId);

            if (userLocked)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 420,
                    Content = JsonConvert.SerializeObject(new
                    {
                        refused = "Server refuses to serve this request: User is locked" 
                    })
                };
                return;
            }
            lock (LockedUsers)
            {
                LockedUsers.Add(userId);
            }

            base.OnActionExecuting(context);
        }
    }
}
