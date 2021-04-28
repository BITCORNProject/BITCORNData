namespace BITCORNService.Games.Models
{
    public class BattlegroundsCreateGameRequest
	{
		public decimal Payin { get; set; }
		public decimal Reward { get; set; }
		public int RewardMultiplier { get; set; }
		public int MaxPlayerCount { get; set; }
		public bool Tournament { get; set; }
		public int? TournamentMapCount { get; set; }
	}
}
