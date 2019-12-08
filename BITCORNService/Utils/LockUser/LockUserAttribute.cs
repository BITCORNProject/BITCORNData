using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace BITCORNService.Utils.LockUser
{
    public class LockUserAttribute : IAsyncActionFilter
    {
        public static HashSet<int> LockedUsers = new HashSet<int>();
        private readonly BitcornContext _dbContext;

        public LockUserAttribute(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            int userId = 0;
            try
            {
                userId = await LockUserAttributeUtils.GetUserId(context, _dbContext);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
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
        }
    }

}
