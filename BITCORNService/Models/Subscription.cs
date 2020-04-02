using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DiscordGuildId { get; set; }
        public int Duration { get; set; }
        public int? OwnerUserId { get; set; }
        public decimal? ReferrerPercentage { get; set; }
    }
}
