using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BITCORNService.Models;
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
            }
        }

        public static async Task Tipped()
        {
            throw new NotImplementedException();
        }

    }
}
