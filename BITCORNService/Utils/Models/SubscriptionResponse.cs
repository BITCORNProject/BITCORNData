using BITCORNService.Models;
using System;

namespace BITCORNService.Utils.Models
{
    public class SubscriptionResponse
    {
        public int? TxId { get; set; }

        public SelectableUser User { get; set; }

        public DateTime? SubscriptionEndTime { get; set; }

        public Subscription RequestedSubscriptionInfo { get; set; }

        public SubscriptionTier RequestedSubscriptionTier { get; set; }
        public double DaysLeft { get; set; }
        public decimal Cost { get; set; }
        public UserSubcriptionTierInfo UserSubscriptionInfo { get; set; }
    }
    
}
