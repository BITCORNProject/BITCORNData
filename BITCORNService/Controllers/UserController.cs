using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public UserController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }

        // POST: api/User
        [HttpPost("{id}")]
        public async Task<FullUser> Post([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            
            return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
        }
        [HttpPost("ban/{id}")]
        public async Task<object> Ban([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var primaryKey = -1;

            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                primaryKey = user.UserId;
                user.IsBanned = true;
                _dbContext.Update(user);

                await _dbContext.SaveAsync();
            }
            var obj = await UserReflection.GetColumns(_dbContext, new string[] { "*" }, new[] { primaryKey });
            return obj.First();
        }
        [HttpGet("{name}/[action]")]
        public bool Check(string name)
        {
            return _dbContext.User.Any(u => u.Username == name);
        }

        [HttpPut("[action]")]
        public async Task<bool> Update([FromBody] Auth0IdUsername auth0IdUsername)
        {
            if (_dbContext.User.Any(u => u.Username == auth0IdUsername.Username))
            {
                return false;
            }
            //join identity with user table to select in 1 query
            var user = await _dbContext.Auth0Query(auth0IdUsername.Auth0Id)
                .Join(_dbContext.User,identity=>identity.UserId,us=>us.UserId,(id,u)=> u).FirstOrDefaultAsync();
          
            user.Username = auth0IdUsername.Username;
            await _dbContext.SaveAsync();
            return true;
        }

    }
}
