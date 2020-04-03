using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public partial class User 
    {
        public User()
        {
            UnclaimedTxReceiverUser = new HashSet<UnclaimedTx>();
            UnclaimedTxSenderUser = new HashSet<UnclaimedTx>();
        }
        [Key]
        public int UserId { get; set; }
        public string Level { get; set; }
        public int SubTier { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? CreationTime { get; set; }
        public virtual UserIdentity UserIdentity { get; set; }
        public virtual UserStat UserStat { get; set; }
        public virtual UserWallet UserWallet { get; set; }
        public virtual UserReferral UserReferral { get; set; }
        public virtual Referrer Referral { get; set; }
        public virtual UserSubscription UserSubscription { get; set; }
        
        public virtual ICollection<UnclaimedTx> UnclaimedTxReceiverUser { get; set; }
        public virtual ICollection<UnclaimedTx> UnclaimedTxSenderUser { get; set; }
    }
}
