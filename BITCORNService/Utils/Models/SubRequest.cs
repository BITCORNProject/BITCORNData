using BITCORNService.Models;
using BITCORNService.Utils.Tx;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class SubRequest : ITxRequest
    {
        public string Platform { get; set; }

        public string SubscriptionName { get; set; }

        public int Tier { get; set; }

        [JsonIgnore]
        public User FromUser { get; set; }

        public decimal Amount { get; set; }

        string ITxRequest.TxType => "$sub";

        IEnumerable<string> ITxRequest.To => new string[] { $"userid|{TxUtils.BitcornHubPK}" };

        public string[] Columns { get; set; }
    }
}
