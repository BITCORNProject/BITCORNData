using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.CompilerServices;

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
                if (userStats.TopTiped < amount)
                {
                    userStats.TopTiped = amount;
                }
                await dbContext.SaveAsync();
            }
        }

    }
}
