using System;
using System.Threading.Tasks;
using BITCORNService.Utils.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace BITCORNService.Utils
{
    public static class DiscordApi
    {
        public static string GetDiscordBotToken(IConfiguration config)
        {
            return config["Config:DiscordBotToken"];
        }
        public static async Task<DiscordUser> GetDiscordUser(string discordBotToken,string discordId)
        {
            var client = new RestClient(@"https://discordapp.com/api");
            var request = new RestRequest($"/users/{discordId}", Method.GET);
            request.AddHeader("Authorization", $"Bot {discordBotToken}");
            var response = await client.ExecuteGetTaskAsync(request);
            return JsonConvert.DeserializeObject<DiscordUser>(response.Content);
        }

        public static string GetUsernameString(DiscordUser discordUser)
        {
            return $"{discordUser.Username}#{discordUser.Discriminator}";
        }
    }
}
