using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using Newtonsoft.Json;
using RestSharp;

namespace BITCORNService.Utils
{
    public static class TwitchKraken
    {
        /// <summary>
        /// Gets a twitch uyser from their ID using the twitch Kraken API
        /// </summary>
        /// <param name="twitchId">The twitch user ID</param>
        /// <returns></returns>
        public static async Task<TwitchUser> GetTwitchUser(string twitchId) 
        {
            var client = new RestClient(@"https://api.twitch.tv/kraken");
            var request = new RestRequest($"/users/{twitchId}", Method.GET);
            request.AddHeader("Client-ID", "wnl8ofrydt837n586ig3nmus2e8ezu");
            request.AddHeader("Accept", "application/vnd.twitchtv.v5+json");
            var response = await client.ExecuteGetTaskAsync(request);
            return JsonConvert.DeserializeObject<TwitchUser>(response.Content);
        }

    }
}
