using BITCORNService.Models;
using BITCORNService.Utils.Tx;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class SubRequest 
    {
        public string Platform { get; set; }
        public string Id { get; set; }
        public string SubscriptionName { get; set; }

        public int Tier { get; set; }

        public decimal Amount { get; set; }
        
        public string[] Columns { get; set; }
    }
}
