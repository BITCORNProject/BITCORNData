namespace BITCORNService.Utils.Models
{
    public class SubTierDiscord
    {
        public string DiscordId { get; set; }
        public int SubTier { get; set; }
        public SubTierDiscord(string discordId, int subTier)
        {
            this.DiscordId = discordId;
            this.SubTier = subTier;
        }
    }

}
