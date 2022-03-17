using Microsoft.AspNetCore.Mvc;
using System;

namespace BITCORNService.Utils.Models
{
    public class FullUser
    {
        public int UserId { get; set; }
        public string Level { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
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
        public int? TotalReferrals { get; set; }
        
        public int? WalletServer { get; set; }
        public decimal? Balance { get; set; }
        public string CornAddy { get; set; }
        public int SubTier { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? CreationTime { get; set; }
        public bool Mfa { get; set; }
        public string YoutubeId { get; set; }
        public string YoutubeUsername { get; set; }
        public string RallyId { get; set; }
        public string RallyUsername { get; set; }
        public RallyUserWallet[] wallets { get; set; } = new RallyUserWallet[0];
    }
}
