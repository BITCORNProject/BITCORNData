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
            userStats.AmountOfTipsSent += 1;
            userStats.TotalSentBitcornViaTips += amount;
            if (userStats.LargestSentBitcornTip < amount)
            {
                userStats.LargestSentBitcornTip = amount;
            }
        }

        public static void Tipped(UserStat userStats, decimal amount)
        {
            userStats.AmountOfTipsReceived += 1;
            userStats.TotalReceivedBitcornTips += amount;

            if (userStats.LargestReceivedBitcornTip < amount)
            {
                userStats.LargestReceivedBitcornTip = amount;
            }
        }

        public static void Rain(UserStat userStats, decimal amount)
        {
            userStats.AmountOfRainsSent += 1;
            userStats.TotalSentBitcornViaRains += amount;
            if (userStats.LargestSentBitcornRain < amount)
            {
                userStats.LargestSentBitcornRain = amount;
            }
        }

        public static void RainedOn(UserStat userStats, decimal amount)
        {
            userStats.AmountOfRainsReceived += 1;
            userStats.TotalReceivedBitcornRains += amount;
            if (userStats.LargestReceivedBitcornRain < amount)
            {
                userStats.LargestReceivedBitcornRain = amount;
            }
        }

        public static void EarnedIdle(UserStat userStats, decimal amount)
        {
            userStats.EarnedIdle += amount;
        }
    }
}
