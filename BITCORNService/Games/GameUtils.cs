using BITCORNService.Games.Models;
using BITCORNService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games
{
    public static class GameUtils
    {
        public static void MigrateUser(BitcornGameContext dbContext, User user, User delete)
        {
            /*
            var oldStats = dbContext.BattlegroundsGameStats.FirstOrDefault(s => s.UserId == delete.UserId);
            if (oldStats != null)
            {
                var newStats = dbContext.BattlegroundsGameStats.FirstOrDefault(s => s.UserId == user.UserId);
                if (newStats == null)
                {
                    newStats = new BattlegroundsUserStats();
                    newStats.UserId = user.UserId;
                    dbContext.BattlegroundsGameStats.Add(newStats);
                }
                newStats.Add(oldStats);
                newStats.GamesPlayed += oldStats.GamesPlayed;
                newStats.Wins += oldStats.Wins;
                dbContext.BattlegroundsGameStats.Remove(oldStats);
            }*/
        }
    }
}
