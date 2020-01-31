namespace BITCORNService.Models
{
    public partial class UserStat
    {
        public int UserId { get; set; }

        /// <summary>
        ///how many times tipped 
        /// </summary>
        public int? AmountOfTipsReceived { get; set; }

        /// <summary>
        /// how much tipped total 
        /// </summary>
        public decimal? TotalReceivedBitcornTips { get; set; }
        /// <summary>
        /// biggest received tip
        /// </summary>
        public decimal? LargestReceivedBitcornTip { get; set; }
        /// <summary>
        /// how many times tipped
        /// </summary>
        public int? AmountOfTipsSent { get; set; }
        /// <summary>
        /// how much you have tipped
        /// </summary>
        public decimal? TotalSentBitcornViaTips { get; set; }
        /// <summary>
        /// biggest tip
        /// </summary>
        public decimal? LargestSentBitcornTip { get; set; }
        /// <summary>
        /// how many times rained
        /// </summary>
        public int? AmountOfRainsSent { get; set; }
        /// <summary>
        /// how much rained total
        /// </summary>
        public decimal? TotalSentBitcornViaRains { get; set; }
        /// <summary>
        /// biggest rain 
        /// </summary>
        public decimal? LargestSentBitcornRain { get; set; }
        /// <summary>
        /// how many times rained on 
        /// </summary>
        public int? AmountOfRainsReceived { get; set; }
        /// <summary>
        /// how much rained on total
        /// </summary>
        public decimal? TotalReceivedBitcornRains { get; set; }
        /// <summary>
        /// biggest received rain
        /// </summary>
        public decimal? LargestReceivedBitcornRain { get; set; }
        /// <summary>
        /// how much earned idling
        /// </summary>
        public decimal? EarnedIdle { get; set; }

        public virtual User User { get; set; }
    }
}
