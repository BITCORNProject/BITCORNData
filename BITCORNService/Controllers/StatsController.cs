using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Stats;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public StatsController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("userstats/{id}")]
        public async Task<UserStat> UserStats([FromRoute] string id)
        {
            if(string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException();

            var platformId = Utils.BitcornUtils.GetPlatformId(id);
            var userIdentity = await Utils.BitcornUtils.GetUserIdentityForPlatform(platformId, _dbContext);

            return await _dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userIdentity.UserId);
        }

        [HttpPost("ReceivedTotal")]
        public async Task<decimal> ReceiverTotal([FromRoute] string auth0Id)
        {
            if (string.IsNullOrWhiteSpace(auth0Id)) throw new ArgumentNullException();

                var userIdentity = await _dbContext.Auth0Async(auth0Id);
                var userStat =  await _dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userIdentity.UserId);
                return Convert.ToDecimal(userStat.RainedOnTotal + userStat.TippedTotal);
        }

    }
}
