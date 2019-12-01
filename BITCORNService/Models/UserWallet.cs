namespace BITCORNService.Models
{
    public partial class UserWallet
    {
        public int UserId { get; set; }
        public int? WalletServer { get; set; }
        public decimal? Balance { get; set; }
        public string CornAddy { get; set; }

        public virtual User User { get; set; }
    }
}
