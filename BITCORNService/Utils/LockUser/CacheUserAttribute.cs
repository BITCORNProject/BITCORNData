using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
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
        
        public static async Task<User> ReadUser(IConfiguration config, BitcornContext dbContext, ActionExecutingContext context)
        {
            var identity = context.HttpContext.User.Identities.First();
            var claim = identity.Claims.FirstOrDefault(c => c.Type == config["Config:IdKey"]);
            if (claim == default(Claim)) return null;
            var split = claim.Value.Split('@');
            if (split.Length == 1)
            {
                var user= await dbContext.Auth0Query(claim.Value).FirstOrDefaultAsync();
                if(user!=null)
                    context.HttpContext.Items.Add("user", user);
                return user;
            }
            else
            {
                return null;
            }

        }
    }
}
