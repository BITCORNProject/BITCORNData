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
        public int? Tipped { get; set; }
        public decimal? TippedTotal { get; set; }
        public decimal? TopTipped { get; set; }
        public int? Tip { get; set; }
        public decimal? TipTotal { get; set; }
        public decimal? TopTip { get; set; }
        public int? Rained { get; set; }
        public decimal? RainTotal { get; set; }
        public decimal? TopRain { get; set; }
        public int? RainedOn { get; set; }
        public decimal? RainedOnTotal { get; set; }
        public decimal? TopRainedOn { get; set; }
        public decimal? EarnedIdle { get; set; }
        public int? WalletServer { get; set; }
        public decimal? Balance { get; set; }
        public string CornAddy { get; set; }
    }
}
