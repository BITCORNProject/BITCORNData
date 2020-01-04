namespace BITCORNService.Models
{
    public class WalletServer
    {
        public int Id { get; set; }
        public string Endpoint { get; set; }
        public int Index { get; set; }
        public string LastBalanceUpdateBlock { get; set; }
        public string DepositAddress { get; set; }
        public decimal? ServerBalance { get; set; }
        public bool Enabled { get; set; }
        public bool WithdrawEnabled { get; set; }
    }
}
