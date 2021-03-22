using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Games;
using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Authorize(Policy = AuthScopes.ReadUser)]
    [Route("api/[controller]")]
    [ApiController]
    public class UserAvatarController : ControllerBase
    {
        BitcornGameContext _dbContext;
        public UserAvatarController(BitcornGameContext dbContext)
        {
            this._dbContext = dbContext;
        }
        public class GetAvatarsBody
        {
            public string[] Names { get; set; }
        }

        [HttpPost("twitchavatars")]
        public async Task<ActionResult<UserAvatarOutputTwitchName[]>> GetAvatarsForTwitch([FromBody] GetAvatarsBody body)
        {
            var avatarConfig = await _dbContext.AvatarConfig.FirstOrDefaultAsync(c => c.Platform == "webgl");
            var names = body.Names;
            var identities = await _dbContext.UserIdentity.Where(x=>names.Contains(x.TwitchUsername)).ToArrayAsync();
            var userIds = identities.Select(x => x.UserId).ToArray();

            var foundAvatars = await _dbContext.UserAvatar.Where(x=>userIds.Contains(x.UserId)).ToArrayAsync();
            //var foundIds = foundAvatars.Select(x=>x.UserId).ToArray();
            int newAvatars = 0;
            var outputs = new List<UserAvatarOutputTwitchName>();
            for (int i = 0; i < identities.Length; i++)
            {
                UserAvatar avatar = null;
                bool found = false;
                for (int j = 0; j < foundAvatars.Length; j++)
                {
                    var foundAvatar = foundAvatars[j];
                    if(foundAvatar.UserId==userIds[i])
                    {
                        avatar = foundAvatar;
                        found = true;
                        break;
                    }
                }

                if(!found)
                {
                    avatar = new UserAvatar();
                    avatar.UserId = userIds[i];
                    avatar.AvatarAddress = avatarConfig.DefaultAvatar;

                    _dbContext.UserAvatar.Add(avatar);
                    newAvatars++;
                }

                outputs.Add(new UserAvatarOutputTwitchName() { 
                    AvailableAvatars = new string[0],
                    Avatar = avatar.AvatarAddress,
                    Catalog = avatarConfig.Catalog,
                    Name = identities[i].TwitchUsername
                });
            }

            if(newAvatars>0)
            {
                await _dbContext.SaveAsync();
            }


            return outputs.ToArray();
        }

        [HttpGet("{platform}/{id}")]
        public async Task<ActionResult<object>> Get([FromRoute] string platform, [FromRoute] string id)
        {
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                //join wallet in dbContext.UserWallet on identity.UserId equals wallet.UserId
                /*var userQuery = BitcornUtils.GetUserForPlatform(platformId, _dbContext).Join(_dbContext.UserAvatar,
                    (User user) => user.UserId,
                    (UserAvatar avatar) => avatar.UserId, (us, av) => new
                    {
                        avatar = av,
                        user = us
                    });
                var userData = await userQuery.FirstOrDefaultAsync();*/
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    return await GameUtils.GetAvatar(_dbContext, user, platform);
                }
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
            /*
             * 
            User user = null;
            if ((user = this.GetCachedUser()) == null)
                return StatusCode(404);*/
            //return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
        }
    }
}