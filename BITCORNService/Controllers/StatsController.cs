using System;
using System.Linq;
using System.Net;
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

        [HttpGet("leaderboard/{orderby}/{nameProvider}")]
        public async Task<ActionResult<object>> Leaderboard([FromRoute] string orderby, [FromRoute] string nameProvider)
        {
            var properties = typeof(UserStat)
                    .GetProperties()
                    .Select(p => p.Name.ToLower())
                    .ToArray();

            if (properties.Contains(orderby.ToLower()))
            {
                var query = _dbContext.UserStat.OrderByDescending(orderby).Join(_dbContext.UserIdentity,
                               (stats) => stats.UserId,
                               (identity) => identity.UserId,
                               (selectedStats, userIdentity) => new
                               {
                                   identity = userIdentity,
                                   stats = selectedStats
                               }).Join(_dbContext.User,
                               (info) => info.stats.UserId,
                               (user) => user.UserId,
                               (selectedInfo, user) => new
                               {
                                   identity = selectedInfo.identity,
                                   stats = selectedInfo.stats,
                                   isBanned = user.IsBanned
                               })
                               .Where(u => !u.isBanned && u.identity.UserId != Utils.Tx.TxUtils.BitcornHubPK);

                if (!string.IsNullOrEmpty(nameProvider))
                {
                    var name = nameProvider.ToLower();
                    if (name == "twitchusername")
                        query = query.Where(u => u.identity.TwitchUsername != "" && u.identity.TwitchUsername != null);
                    else if (name == "discordusername")
                        query = query.Where(u => u.identity.DiscordUsername != "" && u.identity.DiscordUsername != null);
                    else if (name == "twitterusername")
                        query = query.Where(u => u.identity.TwitterUsername != "" && u.identity.TwitterUsername != null);
                    else if (name == "redditusername")
                        query = query.Where(u => u.identity.RedditId != "" && u.identity.RedditId != null);
                }

                return await query.Take(100).ToArrayAsync();
            }
            return StatusCode((int)HttpStatusCode.BadRequest);

        }
    }
}
