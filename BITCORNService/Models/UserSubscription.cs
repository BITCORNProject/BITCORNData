using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class UserSubscription
    {
        [Key]
        public int UserSubscriptionId { get; set; }
        [ForeignKey("FK_usersubscription_userId")]
        public int UserId { get; set; }
        public DateTime? FarmsSubDate { get; set; }
        public virtual User User { get; set; }
    }
}
