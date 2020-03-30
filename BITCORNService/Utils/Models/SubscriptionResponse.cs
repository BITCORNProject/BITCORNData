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
    }
}
