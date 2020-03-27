using Newtonsoft.Json;
using System;

namespace BITCORNService.Utils.Models
{
    public class TwitterUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
        public DateTime? GetCreatedTime()
        {
            if (!string.IsNullOrEmpty(CreatedAt))
            {
                return DateTime.ParseExact(CreatedAt, "ddd MMM dd HH:mm:ss +0000 yyyy", System.Globalization.CultureInfo.InvariantCulture);
            }
            return null;
        }
    }
}
