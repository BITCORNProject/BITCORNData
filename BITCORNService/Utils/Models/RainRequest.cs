using BITCORNService.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Models
{
    public class RainRequest : ITxRequest
    {
        public string From { get; set; }

        public decimal Amount { get; set; }

        public string Platform { get; set; }

        public IEnumerable<string> To { get; set; }

        public string[] Columns { get; set; }

        string ITxRequest.TxType => "$rain";

        [JsonIgnore]
        public User FromUser { get; set; }

    }
}
