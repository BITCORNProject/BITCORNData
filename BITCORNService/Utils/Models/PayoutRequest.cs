using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class PayoutRequest
    {
        public string IrcTarget { get; set; }
        public HashSet<string> Chatters { get; set; }
        public decimal Minutes { get; set; }
    }
}
