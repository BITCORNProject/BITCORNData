using BITCORNService.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games.Models
{
	public class BattlegroundsUser : BattlegroundsGameStats
	{
		[Key]
		public int Id { get; set; }
		public int HostId { get; set; }
		public int GamesPlayed { get; set; }
		public int Wins { get; set; }
		public int CurrentGameId { get; set; }
		public string CurrentTournamentId { get; set; }
		public int TournamentWins { get; set; }
		public int TournamentTeamWins { get; set; }
		public int TeamWins { get; set; }
		public int TournamentsPlayed { get; set; }
		public int VerifiedGameId { get; set; }
		public int? Team { get; set; }
		public decimal TotalCornRewards { get; set; }
		public decimal HostCornRewards { get; set; }
		public bool IsSub { get; set; }
	}
}
