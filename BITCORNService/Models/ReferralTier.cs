using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class ReferralTier
    {
        [Key]
        public int TierId { get; set; }
        public int Tier { get; set; }
        public int Bonus { get; set; }
    }
}
