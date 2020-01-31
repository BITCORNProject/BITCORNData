using System;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Authorize]
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
            return await Utils.BitcornUtils.GetUserForPlatform(platformId, _dbContext).Select(u=>u.UserStat).FirstOrDefaultAsync();
        }

        [HttpPost("ReceivedTotal")]
        public async Task<decimal> ReceiverTotal([FromRoute] string auth0Id)
        {
            if (string.IsNullOrWhiteSpace(auth0Id)) throw new ArgumentNullException();

            var user = await _dbContext.Auth0Query(auth0Id).FirstOrDefaultAsync();
            return Convert.ToDecimal(user.UserStat.TotalReceivedBitcornRains + user.UserStat.TotalReceivedBitcornTips);
        }

    }
}
