using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class SubscriptionTier
    {
        [Key]
        public int SubscriptionTierId { get; set; }
        public int SubscriptionId { get; set; }
        public int Tier { get; set; }
        public string Data { get; set; }
        public decimal? CostCorn { get; set; }
        public decimal? CostUsdt { get; set; }
        public decimal? CostUsd { get; set; }

    }
}
