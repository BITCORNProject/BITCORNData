namespace BITCORNService.Games.Models
{
    public class PickupCornRequest
    {
		public int UserId { get; set; }
		public string Key { get; set; }
		public string IrcTarget { get; set; }
    }
}
