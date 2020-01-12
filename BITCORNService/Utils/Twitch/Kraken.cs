using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Utils.Models;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace BITCORNService.Utils.Twitch
{
    public class Kraken
    {
        private IConfiguration _config;
        private string _refreshToken;
        private string _twitchToken = "eh2021s7mbedwgs03s8012bvk4ly8p";

        public Kraken(IConfiguration configuration)
        {
            _config = configuration;
            
            _refreshToken = _config.GetSection("Config").GetSection("TwitchSubOAuthRefreshToken").Value;
        }



        public async Task Nachos()
        {
            var subs = new List<TwitchSub>();
            var limit = 100;
            var offset = 0;
            var total = 1000;
            var token = RefreshToken();

            for (var count = 0; count <= total; count += 100)
            {
//create client
            var restClient = new RestClient(@"https://api.twitch.tv/");
                
                //create request
                var request = new RestRequest(Method.GET);
                request.Resource = "kraken/channels/223836682/subscriptions";
                request.AddQueryParameter("limit", limit.ToString());
                request.AddQueryParameter("offset", offset.ToString());
                request.AddHeader("Authorization", $"OAuth {token}");
                request.AddHeader("Accept", "application/vnd.twitchtv.v5+json");
                request.AddHeader("Client-ID", _config.GetSection("Config").GetSection("TwitchClientIdSub").Value);
                
                //make request
                var response = restClient.Execute(request);
                var data = JsonConvert.DeserializeObject<TwitchSubData>(response.Content);
                foreach (var subscription in data.subscriptions)
                {
                    subs.Add(new TwitchSub()
                    {
                        Tier = subscription.sub_plan,
                        Id = subscription.user._id

                    });
                }
                total = data._total;
                offset += 100;
            }

            Console.WriteLine(subs);

//for each sub create object with twitchid and subtier



        }

        public string RefreshToken()
        {
            var restClient = new RestClient(@"https://id.twitch.tv");

            //create request
            var request = new RestRequest(Method.POST);
            request.Resource = "oauth2/token";
            request.AddQueryParameter("grant_type", "refresh_token");
            request.AddQueryParameter("refresh_token", _refreshToken);
            request.AddQueryParameter("client_id", _config.GetSection("Config").GetSection("TwitchClientIdSub").Value);
            request.AddQueryParameter("client_secret", _config.GetSection("Config").GetSection("TwitchClientSecretSub").Value);
            request.AddQueryParameter("scope", "openid channel_subscriptions");

            var response = restClient.Execute(request);
            var twitchRefreshData = JsonConvert.DeserializeObject<TwitchRefreshToken>(response.Content);
            

            if (!string.IsNullOrWhiteSpace(twitchRefreshData?.AccessToken) && !string.IsNullOrWhiteSpace(twitchRefreshData?.RefreshToken))
            {
                _twitchToken = twitchRefreshData.AccessToken;
                _refreshToken = twitchRefreshData.RefreshToken;
            }

            return twitchRefreshData.AccessToken;
        }
    }
}
