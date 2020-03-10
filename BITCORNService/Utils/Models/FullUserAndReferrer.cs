using System;

namespace BITCORNService.Utils.Models
{
    public class FullUserAndReferrer
    {
        //User
        public int UserId { get; set; }
        public string Level { get; set; }
        public int SubTier { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public bool IsBanned { get; set; }

        //UserIdentity
        public string TwitchUsername { get; set; }
        public string TwitterUsername { get; set; }
        public string DiscordUsername { get; set; }
        public string RedditUsername => RedditId;
        public string Auth0Nickname { get; set; }
        public string Auth0Id { get; set; }
        public string TwitchId { get; set; }
        public string DiscordId { get; set; }
        public string TwitterId { get; set; }
        public string RedditId { get; set; }

        //UserStat
        public int? AmountOfTipsReceived { get; set; }
        public decimal? TotalReceivedBitcornTips { get; set; }
        public decimal? LargestReceivedBitcornTip { get; set; }
        public int? AmountOfTipsSent { get; set; }
        public decimal? TotalSentBitcornViaTips { get; set; }
        public decimal? LargestSentBitcornTip { get; set; }
        public int? AmountOfRainsSent { get; set; }
        public decimal? TotalSentBitcornViaRains { get; set; }
        public decimal? LargestSentBitcornRain { get; set; }
        public int? AmountOfRainsReceived { get; set; }
        public decimal? TotalReceivedBitcornRains { get; set; }
        public decimal? LargestReceivedBitcornRain { get; set; }
        public decimal? EarnedIdle { get; set; }
        public decimal? TotalReferralRewardsCorn { get; set; }
        public decimal? TotalReferralRewardsUsdt { get; set; }
        public int TotalReferrals { get; set; }
       
        //UserWallet
        public int? WalletServer { get; set; }
        public decimal? Balance { get; set; }
        public string CornAddy { get; set; }
        
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
