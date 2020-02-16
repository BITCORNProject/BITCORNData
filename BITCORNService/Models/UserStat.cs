namespace BITCORNService.Models
{
    public partial class UserStat
    {
        public int UserId { get; set; }
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
        public decimal? ReferralReward { get; set; }

        public virtual User User { get; set; }
    }
}
