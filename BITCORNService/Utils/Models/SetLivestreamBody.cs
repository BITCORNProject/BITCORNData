namespace BITCORNService.Utils.Models
{
    public class SetLivestreamBody
    {
        public bool Enabled { get; set; }
        public bool Public { get; set; }
        public bool EnableTransactions { get; set; }

        public decimal TxCooldownPerUser { get; set; }
        public int RainAlgorithm { get; set; }
        public decimal MinTipAmount { get; set; }
        public decimal MinRainAmount { get; set; }
        public bool TxMessages { get; set; }
        public bool IrcEventPayments { get; set; }

        public decimal Tier3IdlePerMinute { get; set; }
        public decimal Tier2IdlePerMinute { get; set; }
        public decimal Tier1IdlePerMinute { get; set; }

        public decimal BitcornPerBit { get; set; }
        public decimal BitcornPerDonation { get; set; }

        public decimal BitcornPerChannelpointsRedemption { get; set; }
        public bool EnableChannelpoints { get; set; }
    }
}
