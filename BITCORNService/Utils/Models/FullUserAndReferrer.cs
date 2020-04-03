using System;

namespace BITCORNService.Utils.Models
{
    public class FullUserAndReferrer : FullUser
    {
        //referrer
        public int ReferralId { get; set; }
        public decimal Amount { get; set; }
        public int Tier { get; set; }
        public string ETag { get; set; }
        public string Key { get; set; }
        public decimal YtdTotal { get; set; }

        //UserReferral
        public DateTime? WalletDownloadDate { get; set; }
        public DateTime? MinimumBalanceDate { get; set; }
        public DateTime? SyncDate { get; set; }
        public DateTime? SignupReward { get; set; }
        public DateTime? Bonus { get; set; }
        public DateTime? ReferrerBonus { get; set; }
    }
}
