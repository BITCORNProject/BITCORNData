using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class ReferralTx
    {
        [Key]
        public int ReferralTxId { get; set; }
        [ForeignKey("ReferrerUserId")]
        public int UserId { get; set; }

        public decimal Amount { get; set; }
        public decimal UsdtPrice { get; set; }
        public decimal TotalUsdtValue { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Type { get; set; }
        public virtual User User { get; set; }
    }
}
