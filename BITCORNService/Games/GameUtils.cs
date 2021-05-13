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
        public static async Task<UserAvatarOutput> GetAvatar(BitcornGameContext dbContext, User user, string platform)
        {
            var avatarConfig = await dbContext.AvatarConfig.FirstOrDefaultAsync(c => c.Platform == platform);
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

            return new UserAvatarOutput()
            {
                Catalog = avatarConfig.Catalog,
                Avatar = userAvatar.AvatarAddress,
                AvailableAvatars = new string[] { }
            };
        }

        public static async Task<Dictionary<int, UserAvatarOutput>> GetAvatars(BitcornGameContext dbContext, int[] users, string platform)
        {
            var avatarConfig = await dbContext.AvatarConfig.FirstOrDefaultAsync(c => c.Platform == platform);
            var userAvatars = await dbContext.UserAvatar.Where(u => users.Contains(u.UserId)).ToDictionaryAsync(x => x.UserId, x => x);
            int adds = 0;
            for (int i = 0; i < users.Length; i++)
            {
                if (!userAvatars.ContainsKey(users[i]))
                {
                    var userAvatar = new UserAvatar()
                    {
                        AvatarAddress = avatarConfig.DefaultAvatar,
                        UserId = users[i]
                    };
                    dbContext.Add(userAvatar);
                    userAvatars.Add(users[i], userAvatar);
                    adds++;
                }
            }

            if (adds > 0)
            {
                await dbContext.SaveAsync();

            }

            return userAvatars.Values.Select(x =>new UserAvatarOutput()
            {
                UserId = x.UserId,
                Catalog = avatarConfig.Catalog,
                Avatar =x.AvatarAddress,
                AvailableAvatars = new string[] { }
            }).ToDictionary(x=>x.UserId, x=>x);
            //var output = new Dictionary<int, UserAvatarOutput>();

            /*
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

            return new UserAvatarOutput()
            {
                Catalog = avatarConfig.Catalog,
                Avatar = userAvatar.AvatarAddress,
                AvailableAvatars = new string[] { }
            };
            */
        }


    }
}
