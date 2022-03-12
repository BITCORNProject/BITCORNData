using System.Threading.Tasks;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Twitch;
using Newtonsoft.Json;
using RestSharp;

namespace BITCORNService.Utils
{
    public static class TwitchHelix
    {
        /// <summary>
        /// Gets a twitch uyser from their ID using the twitch Helix API
        /// </summary>
        /// <param name="clientId">Twitch app client id</param>
        /// <param name="twitchId">The twitch user ID</param>
        /// <param name="accessToken">Users access token</param>
        /// <returns></returns>
        public static TwitchUserHelix GetTwitchUser(string clientId, string twitchId, string accessToken)
        {
            RestClient restClient = new RestClient(@"https://api.twitch.tv/");
            string resource = $"helix/users?id={twitchId}";

            RestRequest request = new RestRequest(resource, DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Client-ID", clientId);
            IRestResponse response = restClient.Execute(request);
            HelixUserResponse output = JsonConvert.DeserializeObject<HelixUserResponse>(response.Content);
            if (output.data.Length > 0) return output.data[0];
            return null;
        }

    }
}
