namespace BITCORNService.Utils.Models
{
    public class BanUserRequest
    {
        public string Sender { get; set; }
        public string BanUser { get; set; }
    }
    public class UnlockUserWalletRequest
    {
        public string Sender { get; set; }
        public string UnlockUser { get; set; }
    }
}
