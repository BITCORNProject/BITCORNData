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

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

    }
}

