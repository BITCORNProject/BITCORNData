namespace BITCORNService.Games.Models
{
    public class BattlegroundsCreateGameRequest
	{
		public decimal Payin { get; set; }
		public decimal Reward { get; set; }
		public int RewardMultiplier { get; set; }
		public int MaxPlayerCount { get; set; }
		public bool Tournament { get; set; }
		public string[] TournamentMaps { get; set; }
		public int? TournamentPointMethod { get; set; }
		public bool EnableTeams { get; set; }
		public int GameMode { get; set; }
		public bool Bgrains { get; set; }
		public bool JoiningBetweenTournamentGames { get; set; }
		public string MapId { get; set; }
	
		public string Data { get; set; }
	}
}
