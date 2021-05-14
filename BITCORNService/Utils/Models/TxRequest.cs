using BITCORNService.Models;
using System.Collections.Generic;
using System.Linq;

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

        public TxRequest(User from, decimal amount, string platform, string txType, params User[] to)
            : this(from, amount, platform, txType, to.Select(x => "userid|" + x.UserId.ToString()).ToArray())
        {

        }

        public User FromUser { get; set; }
        public decimal Amount { get; set; }
        public string Platform { get; set; }
        public string TxType { get; set; }
        public IEnumerable<string> To { get; set; }
    }

}
