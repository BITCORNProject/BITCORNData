using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils.Stats
{
    public static class UpdateStats
    {
        public static void Tip(UserStat userStats, decimal amount)
        {
            userStats.Tip += 1;
            userStats.TipTotal += amount;
            if (userStats.TopTip < amount)
            {
                userStats.TopTip = amount;
            }
        }

        public static void Tipped(UserStat userStats, decimal amount)
        {
            userStats.Tipped += 1;
            userStats.TippedTotal += amount;

            if (userStats.TopTipped < amount)
            {
                userStats.TopTipped = amount;
            }
        }

        public static void Rain(UserStat userStats, decimal amount)
        {
            userStats.Rained += 1;
            userStats.RainTotal += amount;
            if (userStats.TopRain < amount)
            {
                userStats.TopRain = amount;
            }
        }

        public static void RainedOn(UserStat userStats, decimal amount)
        {
            userStats.RainedOn += 1;
            userStats.RainedOnTotal += amount;
            if (userStats.TopRainedOn < amount)
            {
                userStats.TopRainedOn = amount;
            }
        }

        public static void EarnedIdle(UserStat userStats, decimal amount)
        {
            userStats.EarnedIdle += amount;
        }
    }
}
