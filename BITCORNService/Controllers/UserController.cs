using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    }
}
