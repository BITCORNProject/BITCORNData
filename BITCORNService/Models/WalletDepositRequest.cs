using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class WalletDepositRequest
    {
        public int Index { get; set; }
        public string Block { get; set; }
        public JArray Payments { get; set; }
    }
}
