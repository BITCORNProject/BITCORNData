namespace BITCORNService.Models
{
    public partial class UserStat
    {
        public int UserId { get; set; }

        /// <summary>
        ///how many times tipped 
        /// </summary>
        public int? Tipped { get; set; }

        /// <summary>
        /// how much tipped total 
        /// </summary>
        public decimal? TippedTotal { get; set; }
        /// <summary>
        /// biggest received tip
        /// </summary>
        public decimal? TopTipped { get; set; }
        /// <summary>
        /// how many times tipped
        /// </summary>
        public int? Tip { get; set; }
        /// <summary>
        /// how much you have tipped
        /// </summary>
        public decimal? TipTotal { get; set; }
        /// <summary>
        /// biggest tip
        /// </summary>
        public decimal? TopTip { get; set; }
        /// <summary>
        /// how many times rained
        /// </summary>
        public int? Rained { get; set; }
        /// <summary>
        /// how much rained total
        /// </summary>
        public decimal? RainTotal { get; set; }
        /// <summary>
        /// biggest rain 
        /// </summary>
        public decimal? TopRain { get; set; }
        /// <summary>
        /// how many times rained on 
        /// </summary>
        public int? RainedOn { get; set; }
        /// <summary>
        /// how much rained on total
        /// </summary>
        public decimal? RainedOnTotal { get; set; }
        /// <summary>
        /// biggest received rain
        /// </summary>
        public decimal? TopRainedOn { get; set; }
        /// <summary>
        /// how much earned idling
        /// </summary>
        public decimal? EarnedIdle { get; set; }

        public virtual User User { get; set; }
    }
}
