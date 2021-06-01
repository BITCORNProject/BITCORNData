using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BITCORNService.Utils.LockUser
{
    public class CacheUserAttribute : IAsyncActionFilter
    {

        private readonly BitcornContext _dbContext;
        IConfiguration _config;
        public CacheUserAttribute(IConfiguration config, BitcornContext dbContext)
        {
            this._config = config;
            _dbContext = dbContext;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            User user = await CacheUserAttribute.ReadUser(_config, _dbContext, context);
            ;
            try
            {
                await next();
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, null);
            }
        }
        public static async Task<User> ReadUser(IConfiguration config, BitcornContext dbContext, HttpContext context)
        {
            var identity = context.User.Identities.First();
            var claim = identity.Claims.FirstOrDefault(c => c.Type == config["Config:IdKey"]);
            if (claim == default(Claim)) return null;
            var split = claim.Value.Split('@');
            if (split.Length == 1)
            {
                var user = await dbContext.Auth0Query(claim.Value).FirstOrDefaultAsync();
                if (user != null)
                {
                    context.Items.Add("user", user);
                    context.Items.Add("usermode", 0);

                }
                else
                {
                    if (claim.Value.Contains("auth0|"))
                    {
                        try
                        {
                            user = Controllers.RegisterController.CreateUser(new Auth0User()
                            {
                                Auth0Id = claim.Value,
                                Auth0Nickname = ""
                            }, 0);
                            dbContext.User.Add(user);
                            await dbContext.SaveAsync();
                            context.Items.Add("user", user);
                            context.Items.Add("usermode", 0);
                        }
                        catch (Exception e)
                        {
                            user = null;
                            await BITCORNLogger.LogError(dbContext, e, claim.Value);
                            return null;
                        }
                    }
                }
                //await BITCORNLogger.LogError(dbContext, new Exception(""),
                //Newtonsoft.Json.JsonConvert.SerializeObject(new { userId = claim.Value, isNull=user==null }));
                return user;
            }
            else
            {
                return null;
            }
        }
        public static async Task<User> ReadUser(IConfiguration config, BitcornContext dbContext, ActionExecutingContext context)
        {
            return await ReadUser(config, dbContext, context.HttpContext);

        }
    }
}
