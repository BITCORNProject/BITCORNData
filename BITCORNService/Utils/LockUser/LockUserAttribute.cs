﻿using System;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Utils.LockUser
{
    public class LockUserAttribute : IAsyncActionFilter
    {
        
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
                try
                {
                    var appId = context.HttpContext.GetAppId(_config);
                    if (!string.IsNullOrEmpty(appId))
                    {
                        var thirdPartyClient = await _dbContext.ThirdPartyClient.AnyAsync(a => a.ClientId == appId);
                        if (thirdPartyClient)
                        {
                            context.Result = new ContentResult()
                            {
                                StatusCode = (int)HttpStatusCode.Forbidden,
                                Content = JsonConvert.SerializeObject(new
                                {
                                    refused = "Server refuses to serve this request: invalid headers"
                                })
                            };
                            await BITCORNLogger.LogError(_dbContext, new Exception("Forbidden request for app id:" + appId), appId);
                            return;
                        }
                    }
                }
                catch(Exception e)
                {
                    await BITCORNLogger.LogError(_dbContext,e ,null);

                    throw e;
                }

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
                    if (user != null)
                    {
                        context.HttpContext.Items.Add("user", user);

                        context.HttpContext.Items.Add("usermode", 1);
                    }
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
            if (!UserLockCollection.Lock(user))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = UserLockCollection.UserLockedReturnCode,
                    Content = JsonConvert.SerializeObject(new
                    {
                        refused = "Server refuses to serve this request: User is locked"
                    })
                };
                return;
            }
            try
            {
                await next();
            }
            finally
            {
                UserLockCollection.Release(user);
            }
        }
    }
}
