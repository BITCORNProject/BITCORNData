using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Utils.LockUser
{
    public class LockUserAttribute : IAsyncActionFilter
    {
        public static HashSet<int> LockedUsers = new HashSet<int>();
        private readonly BitcornContext _dbContext;
        IConfiguration _config;
        public LockUserAttribute(IConfiguration config, BitcornContext dbContext)
        {
            this._config = config;
            _dbContext = dbContext;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            User user = await CacheUserAttribute.ReadUser(_config, _dbContext, context);
            try
            {
                if (user == null)
                {
                    var query = LockUserAttributeUtils.GetUserFromHeader(context, _dbContext);
                    if (query == null)
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = (int)HttpStatusCode.BadRequest,
                            Content = JsonConvert.SerializeObject(new
                            {
                                refused = "Server refuses to serve this request: invalid headers"
                            })
                        };
                        return;
                    }
                    user = await query.FirstOrDefaultAsync();
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, null);
                throw;
            }

            if (user == null)
            {
              
                //let the api deal with unregistered sender
                await next();
                return;
            }
            lock (LockedUsers)
            {
                var userLocked = LockedUsers.Contains(user.UserId);

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

                LockedUsers.Add(user.UserId);
            }

            try
            {
                await next();
            }
            finally
            {
                lock (LockedUsers)
                {
                    LockedUsers.Remove(user.UserId);
                }
            }
        }
    }
}
