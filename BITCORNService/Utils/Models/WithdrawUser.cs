using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Models
{
    public class WithdrawUser
    {
        public decimal Amount { get; set; }
        public string CornAddy { get; set; }
        public string Id { get; set; }
    }
}
