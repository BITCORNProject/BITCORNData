using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class UserReferral
    {
        [Key]
        public int UserReferralId { get; set; }
        [ForeignKey("FK_user_userId")]
        public int UserId { get; set; }
        [ForeignKey("FK_referrer_referralId")]
        public int ReferralId { get; set; }
        public DateTime? WalletDownloadDate { get; set; }
        public DateTime? MinimumBalanceDate { get; set; }
        public DateTime? TweetDate { get; set; }
        public DateTime? SyncDate{ get; set; }
        public bool SignupReward { get; set; }

        public bool Bonus { get; set; }
        public bool ReferrerBonus { get; set; }
        public virtual User User { get; set; }
    }
}
