namespace BITCORNService.Games.Models
{
    public class GameInstance
	{
		public int GameId { get; set; }
		public int HostId { get; set; }
		public decimal Payin { get; set; }
		public decimal Reward { get; set; }
		public bool Active { get; set; }
		public int? HostDebitCornTxId { get; set; }
	}
}
