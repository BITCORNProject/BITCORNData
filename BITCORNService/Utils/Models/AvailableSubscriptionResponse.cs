using BITCORNService.Models;
using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class AvailableSubscriptionResponse
    {
        public Subscription Subscription { get; set; }
        public List<object> Tiers { get; set; } = new List<object>();
    }
}
