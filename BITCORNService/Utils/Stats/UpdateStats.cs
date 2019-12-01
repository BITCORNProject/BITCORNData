using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils.Stats
{
    public static class UpdateStats
    {
        public static async Task Tip(int userId, decimal amount)
        {
            using (var dbContext = new BitcornContext())
            {
                var userStats = await dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userId);
                userStats.Tip += 1;
                userStats.TipTotal += amount;
                if (userStats.TopTip < amount)
                {
                    userStats.TopTip = amount;
                }

                await dbContext.SaveAsync();
            }
        }

        public static async Task Tipped(int userId, decimal amount)
        {
            using (var dbContext = new BitcornContext())
            {
                var userStats = await dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userId);
                userStats.Tipped += 1;
                userStats.TippedTotal += amount;

                if (userStats.TopTipped < amount)
                {
                    userStats.TopTipped = amount;
                }
                await dbContext.SaveAsync();
            }
        }

        public static async Task Rain(int userId, decimal amount)
        {
            using (var dbContext = new BitcornContext())
            {
                var userStats = await dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userId);
                userStats.Rained += 1;
                userStats.RainTotal += amount;
                if (userStats.TopRain < amount)
                {
                    userStats.TopRain = amount;
                }
                await dbContext.SaveAsync();
            }
        }

        public static async Task RainedOn(int userId, decimal amount)
        {
            using (var dbContext = new BitcornContext())
            {
                var userStats = await dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userId);
                userStats.RainedOn += 1;
                userStats.RainedOnTotal += amount;
                if (userStats.TopRainedOn < amount)
                {
                    userStats.TopRainedOn = amount;
                }
                await dbContext.SaveAsync();
            }
        }

        public static async Task EarnedIdle(int userId, decimal amount)
        {
            using (var dbContext = new BitcornContext())
            {
                var userStats = await dbContext.UserStat.FirstOrDefaultAsync(u => u.UserId == userId);
                userStats.EarnedIdle += amount;
                await dbContext.SaveAsync();
            }
        }
    }
}
