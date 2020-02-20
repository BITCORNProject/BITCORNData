using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> Get(string id)
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
                    var avatarConfig = await _dbContext.AvatarConfig.FirstOrDefaultAsync();
                    var userAvatar = await _dbContext.UserAvatar.FirstOrDefaultAsync(u=>u.UserId==user.UserId);
                    if (userAvatar == null)
                    {
                        userAvatar = new UserAvatar() {
                            AvatarAddress = "Assets/BitcornAvatar/Remote Scare Bear.prefab",
                            UserId = user.UserId
                        };
                        _dbContext.Add(userAvatar);
                        await _dbContext.SaveAsync();
                    }
                    /*
                    var items = await _dbContext.UserInventoryItem.Where(u => u.UserId == userData.user.UserId && u.Type == "avatar")
                   .Join(_dbContext.ItemPrefab,
                   (UserInventoryItem item) => item.ItemPrefabId,
                   (ItemPrefab prefab) => prefab.Id, (inventoryItem, itemPrefab) => new
                   {
                       itemPrefab.AddressablePath,

                   })
                   .ToArrayAsync();*/


                    return new
                    {
                        catalog = avatarConfig.Catalog,
                        avatar = userAvatar.AvatarAddress,
                        availableAvatars = new string[] { }
                    };
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