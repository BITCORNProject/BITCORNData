using Newtonsoft.Json;

namespace BITCORNService.Utils.Models
{
    public class DiscordUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }
        [JsonProperty("discriminator")]
        public string Discriminator { get; set; }
        [JsonProperty("avatar")]
        public string Avatar { get; set; }
    }
}
