using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class Referrer
    {
        [Key]
        public int ReferralId { get; set; }
        [ForeignKey("UserIdFK")]
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public decimal YtdTotal { get; set; }

        public int Tier { get; set; }
        public string ETag { get; set; }
        public string Key { get; set; }
        public virtual User User { get; set; }
    }
}

