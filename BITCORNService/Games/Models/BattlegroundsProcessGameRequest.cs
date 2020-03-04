namespace BITCORNService.Games.Models
{
    public class BattlegroundsProcessGameRequest
	{
		public BattlegroundsGameStats[] Players { get; set; }
		public int WinnerIndex { get; set; }
	}
	public class BattlegroundsCreateGameRequest
	{
		public decimal Payin { get; set; }
		public decimal Reward { get; set; }
	}
}
