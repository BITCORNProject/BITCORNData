namespace BITCORNService.Games.Models
{
    public class BitcornBattlegroundsProcessGameRequest
	{
		public BattlegroundsGameStats[] Players { get; set; }
		public int WinnerIndex { get; set; }
	}
}
