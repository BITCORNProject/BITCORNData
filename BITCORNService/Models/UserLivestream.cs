using System;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class UserLivestream
    {
        [Key]
        public int UserId { get; set; }
        public bool Enabled { get; set; } 
        public bool Public { get; set; }
        public bool IrcEventPayments { get; set; }
        public string IrcTarget { get; set; }
        public decimal TotalSentBitcornViaTips { get; set; }
        public decimal TotalSentBitcornViaRains { get; set; }
        public decimal TotalBitcornPaidViaIdling { get; set; }
        public DateTime? LastSubTickTimestamp { get; set; }
        public int AmountOfRainsSent { get; set; }
        public int AmountOfTipsSent { get; set; }
        public bool EnableTransactions { get; set; }
        public decimal TxCooldownPerUser { get; set; }
        public int RainAlgorithm { get; set; }
        public decimal MinTipAmount { get; set; }
        public decimal MinRainAmount { get; set; }
        public bool TxMessages { get; set; }
        public bool BitcornhubFunded { get; set; }

        public decimal Tier3IdlePerMinute { get; set; }
        public decimal Tier2IdlePerMinute { get; set; }
        public decimal Tier1IdlePerMinute { get; set; }

        public decimal BitcornPerBit { get; set; }
        public decimal Tier1SubReward { get; set; }
        public decimal Tier2SubReward { get; set; }
        public decimal Tier3SubReward { get; set; }
        public decimal BitcornPerChannelpointsRedemption { get; set; }
        public bool EnableChannelpoints { get; set; }
        public string ChannelPointCardId { get; set; }
        public bool EnableTts { get; set; }
        public decimal BitcornPerTtsCharacter { get; set; }
        public string GiveawayText { get; set; }
        public DateTime? GiveawayEnd { get; set; }
        public bool GiveawayOpen { get; set; }
        public decimal GiveawayEntryFee { get; set; }
        public DateTime? LastUpdateTime { get; set; }
        public int GiveawayIndex { get; set; }
    }
   
    public class UserGiveawayTicket
    {
        [Key]
        public int GiveawayTicketId { get; set; }
        public int UserId { get; set; }
        public int ChannelId { get; set; }
        public int Amount { get; set; }
        public int GiveawayIndex { get; set; }
    }
}
