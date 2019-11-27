using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace BITCORNService.Utils.LockUser
{
    public class LockUserAttribute : ActionFilterAttribute
    {
        public static HashSet<int> LockedUsers = new HashSet<int>();

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
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
            context.HttpContext.Items.Add(new KeyValuePair<object, object>("Id", userId));
            await base.OnActionExecutionAsync(context, next);
        }

<<<<<<< HEAD
<<<<<<< HEAD
            base.OnActionExecuting(context);
=======
=======
>>>>>>> d028daf1fb4ba931e2e06b7d481f6357d4aa0672
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Items.TryGetValue("Id", out object userId);
            lock (LockedUsers)
            {
                LockedUsers.Remove(Convert.ToInt32(userId));
            }
            base.OnActionExecuted(context);
<<<<<<< HEAD
>>>>>>> moved wallet code to utils
=======
>>>>>>> d028daf1fb4ba931e2e06b7d481f6357d4aa0672
        }
    }

}
