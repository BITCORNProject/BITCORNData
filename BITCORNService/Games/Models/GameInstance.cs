using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Games.Models
{
    public class GameInstance
	{
		[Key]
		public int GameId { get; set; }
		public int HostId { get; set; }
		public decimal Payin { get; set; }
		public decimal Reward { get; set; }
		public int RewardMultiplier { get; set; }
		public int PlayerLimit { get; set; }
		public bool Active { get; set; }
		public bool Started { get; set; }
		public int? HostDebitCornTxId { get; set; }
		public string TournamentId { get; set; }
	}
	public class Tournament
	{

		[Key]
		public string TournamentId { get; set; }
		public int UserId { get; set; }
		public int MapCount { get; set; }
		public int MapIndex { get; set; }
		public bool Completed { get; set; }
	}
}
