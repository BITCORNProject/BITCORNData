using BITCORNService.Models;
using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class TxRequest : ITxRequest
    {
        public TxRequest(User from, decimal amount, string platform, string txType, params string[] to)
        {
            this.FromUser = from;
            this.Amount = amount;
            this.Platform = platform;
            this.TxType = txType;
            this.To = to;
        }
        public User FromUser { get; set; }
        public decimal Amount { get; set; }
        public string Platform { get; set; }
        public string TxType { get; set; }
        public IEnumerable<string> To { get; set; }
    }
}
