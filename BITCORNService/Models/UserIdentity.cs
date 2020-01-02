namespace BITCORNService.Models
{
    public partial class UserIdentity
    {
        public int UserId { get; set; }
        public string TwitchUsername { get; set; }
        public string DiscordUsername { get; set; }
        public string TwitterUsername { get; set; }
        public string RedditUsername { get => RedditId; }
        public string Auth0Nickname { get; set; }
        public string Auth0Id { get; set; }
        public string TwitchId { get; set; }
        public string DiscordId { get; set; }
        public string TwitterId { get; set; }
        public string RedditId { get; set; }

        public virtual User User { get; set; }
    }
}
