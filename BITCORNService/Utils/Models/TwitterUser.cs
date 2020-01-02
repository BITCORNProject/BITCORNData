using Newtonsoft.Json;

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
    }
}
