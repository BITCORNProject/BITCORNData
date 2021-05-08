namespace BITCORNService.Games.Models
{
    public class BattlegroundsJoinGameRequest
	{
		public string UserPlatformId { get; set; }
		public int GameId { get; set; }
	}


	public class BattlegroundsJoinGameRequest2
	{
		public string IrcTarget { get; set; }
		public string UserPlatformId { get; set; }
		public bool IsSub { get; set; }
	}
}
