using System.Collections.Generic;

namespace BITCORNService.Models
{
    public partial class User
    {
        public User()
        {
            UnclaimedTxReceiverUser = new HashSet<UnclaimedTx>();
            UnclaimedTxSenderUser = new HashSet<UnclaimedTx>();
        }

        public int UserId { get; set; }
        public string Level { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public bool IsBanned { get; set; }
        public int SubTier { get; set; }
        public virtual UserIdentity UserIdentity { get; set; }
        public virtual UserStat UserStat { get; set; }
        public virtual UserWallet UserWallet { get; set; }
        public virtual ICollection<UnclaimedTx> UnclaimedTxReceiverUser { get; set; }
        public virtual ICollection<UnclaimedTx> UnclaimedTxSenderUser { get; set; }
    }
}
