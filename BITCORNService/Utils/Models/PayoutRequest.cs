using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class PayoutRequest
    {
        public HashSet<string> Chatters { get; set; }
        public decimal Minutes { get; set; }
    }
}
