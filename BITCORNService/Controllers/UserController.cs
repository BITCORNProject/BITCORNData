using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
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
            if(string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var userIdentity = await BitcornUtils.GetUserIdentityForPlatform(platformId, _dbContext);
            if (userIdentity == null) throw new ArgumentNullException("userIdentity");

            var user = _dbContext.User.FirstOrDefault(u => u.UserId == userIdentity.UserId);
            if (user == null) throw new ArgumentNullException("user");

            var userWallet = _dbContext.UserWallet.FirstOrDefault(u => u.UserId == userIdentity.UserId);
            if (userWallet == null) throw new ArgumentNullException("userWallet");

            var userStats = _dbContext.UserStat.FirstOrDefault(u => u.UserId == userIdentity.UserId);
            
            if (userStats == null) throw new ArgumentNullException("userStats");

             return BitcornUtils.GetFullUser(user, userIdentity, userWallet, userStats);
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
            
            var userIdentity = await _dbContext.Auth0Async(auth0IdUsername.Auth0Id);
            var user = await _dbContext.User.FirstOrDefaultAsync(u => u.UserId == userIdentity.UserId);
            user.Username = auth0IdUsername.Username;
            await _dbContext.SaveAsync();
            return true;
        }

    }
}
