using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserWalletController : ControllerBase
    {
        [HttpPost("{twitch}")]
        public async Task<object> Post([FromBody] TwitchBody twitchBody)
        {
            using (var dbContext = new BitcornContext())
            {
                var userIdentity = await dbContext.TwitchAsync(twitchBody.Id);
                var userWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == userIdentity.UserId);

                return userWallet;
            }
        }
    }
}
