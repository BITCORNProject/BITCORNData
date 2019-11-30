using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class WalletWithdrawalRequest
    {
        //TODO: add user id
        public decimal Amount { get; set; }
        public string Cornaddy { get; set; }
    
    }
}
