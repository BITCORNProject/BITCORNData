using System;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Utils.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BITCORNService.Utils
{
    public static class TwitterApi
    {
        public static async Task<string> GetTwitterToken(IConfiguration config)
        {
            var secret = config["Config:TwitterBotSecret"];
           
            var client = new RestClient("https://api.twitter.com");
            var request = new RestRequest("/oauth2/token", Method.POST);
            var credintals = Encoding.UTF8.GetBytes($"ZT4gsBlXU6Ohj3p2w11g1uGH1:{secret}");

            request.AddHeader("Authorization", $"Basic {Convert.ToBase64String(credintals)}");
            request.AddParameter("grant_type", "client_credentials");
 
            var response = await client.ExecutePostTaskAsync(request);
            return JObject.Parse(response.Content)["access_token"].ToString();
        }
        public static async Task<TwitterUser> GetTwitterUser(IConfiguration configuration, string twitterId)
        {
            var token = await GetTwitterToken(configuration);
            var client = new RestClient(@"https://api.twitter.com/1.1");
            var request = new RestRequest($"/users/show.json?user_id={twitterId}", Method.GET);

            request.AddHeader("Authorization", $"Bearer {token}");
            var response = await client.ExecuteGetTaskAsync(request);
            return JsonConvert.DeserializeObject<TwitterUser>(response.Content);
        }

        public static string GetUsernameString(DiscordUser discordUser)
        {
            return $"{discordUser.Username}#{discordUser.Discriminator}";
        }
    }
}
