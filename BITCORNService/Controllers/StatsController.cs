using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        [HttpPost("{userstats}")]
        public async Task<UserStat> UserStats([FromRoute] string auth0Id)
        {
            if(string.IsNullOrWhiteSpace(auth0Id)) throw new ArgumentNullException();

            using (var dbContext = new BitcornContext())
            {
                var userIdentity = await dbContext.Auth0Async(auth0Id);
                return await dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userIdentity.UserId);
            }
        }
    }
}
