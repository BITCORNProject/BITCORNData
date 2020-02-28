using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class ReferralTx
    {
        [Key]
        public int UserId { get; set; }

        public int ReferrerUserId { get; set; }

        public decimal Amount { get; set; }
        public decimal UsdtPrice { get; set; }
        public decimal TotalUsdtValue { get; set; }
        public int ReferralId { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
