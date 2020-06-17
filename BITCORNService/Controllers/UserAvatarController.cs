using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Games;
using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserAvatarController : ControllerBase
    {
        BitcornGameContext _dbContext;
        public UserAvatarController(BitcornGameContext dbContext)
        {
            this._dbContext = dbContext;
        }
        [HttpGet("{platform}/{id}")]
        public async Task<ActionResult<object>> Get([FromRoute]string platform,[FromRoute]string id)
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
                    return await GameUtils.GetAvatar(_dbContext,user, platform);
                }
                throw new NotImplementedException();
            }
            catch(Exception e)
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