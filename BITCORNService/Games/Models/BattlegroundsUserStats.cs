using BITCORNService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games.Models
{
	public class BattlegroundsUserStats : BattlegroundsGameStats
	{
		public int GamesPlayed { get; set; }
		public int Wins { get; set; }
	}
}
