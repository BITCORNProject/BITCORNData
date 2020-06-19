namespace BITCORNService.Models
{
    public class SubTx
    {
        public int SubTxId { get; set; }
        public int UserId { get; set; }
        public int UserSubscriptionId { get; set; }
        public int? ReferralTxId { get; set; }
        public decimal? CostCorn { get; set; }
        public decimal? CostUsd { get; set; }
    }
}
