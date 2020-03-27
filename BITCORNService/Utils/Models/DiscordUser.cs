using Newtonsoft.Json;
using System;

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

        public DateTime? GetCreatedTime()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long.Parse(Id) / 4194304) + 1420070400000);
            }
            return null;
        }
    }
}
