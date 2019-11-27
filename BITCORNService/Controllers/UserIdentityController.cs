using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BITCORNService.Utils.DbActions;
using Microsoft.Extensions.Logging;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserIdentityController : ControllerBase
    {
        [HttpPost("{Auth0Id}")]
        public async Task<UserIdentity> Auth0([FromRoute] string auth0Id)
        {
            using (var dbContext = new BitcornContext())
            {
                return await dbContext.Auth0Async(auth0Id);
            }
        }

        [HttpPost("{TwitchId}")]
        public async Task<UserIdentity> Twitch([FromRoute] string twitchId)
        {
            using (var dbContext = new BitcornContext())
            {
                return await dbContext.TwitchAsync(twitchId);
            }
        }

        [HttpPost("{Discord}")]
        public async Task<UserIdentity> Discord([FromRoute] string discordId)
        {
            using (var dbContext = new BitcornContext())
            {
                return await dbContext.DiscordAsync(discordId);
            }
        }

        [HttpPost("{Twitter}")]
        public async Task<UserIdentity> Twitter([FromRoute] string twitterId)
        {
            using (var dbContext = new BitcornContext())
            {
                return await dbContext.TwitterAsync(twitterId);
            }
        }

        [HttpPost("{Reddit}")]
        public async Task<UserIdentity> Reddit([FromRoute] string redditId)
        {
            using (var dbContext = new BitcornContext())
            {
                return await dbContext.TwitterAsync(redditId);
            }
        }

    }
}