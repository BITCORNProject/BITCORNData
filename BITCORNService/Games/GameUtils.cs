using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games
{
    public static class GameUtils
    {
        public const string AvatarPlatformWindows = "windows";
        public const string AvatarPlatformWebGl = "webgl";
        public static async Task<UserAvatarOutput> GetAvatar(BitcornGameContext dbContext, User user,string platform)
        {
            var avatarConfig = await dbContext.AvatarConfig.FirstOrDefaultAsync(c=>c.Platform==platform);
            var userAvatar = await dbContext.UserAvatar.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (userAvatar == null)
            {
                userAvatar = new UserAvatar()
                {
                    AvatarAddress = avatarConfig.DefaultAvatar,
                    UserId = user.UserId
                };
                dbContext.Add(userAvatar);
                await dbContext.SaveAsync();
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


            return new UserAvatarOutput()
            {
                Catalog = avatarConfig.Catalog,
                Avatar = userAvatar.AvatarAddress,
                AvailableAvatars = new string[] { }
            };
        }
    }
}
