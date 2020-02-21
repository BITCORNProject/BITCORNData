namespace BITCORNService.Games.Models
{
    public class BattlegroundsProcessGameRequest
	{
		public BattlegroundsGameStats[] Players { get; set; }
		public int WinnerIndex { get; set; }
	}
}
