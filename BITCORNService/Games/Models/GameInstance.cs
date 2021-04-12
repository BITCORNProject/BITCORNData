namespace BITCORNService.Games.Models
{
    public class GameInstance
	{
		public int GameId { get; set; }
		public int HostId { get; set; }
		public decimal Payin { get; set; }
		public decimal Reward { get; set; }
		public int RewardMultiplier { get; set; }
		public int PlayerLimit { get; set; }
		public bool Active { get; set; }
		public bool Started { get; set; }
		public int? HostDebitCornTxId { get; set; }
	}
}
