using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace BITCORNService.Utils.Twitch
{
    public class Kraken
    {
        private IConfiguration _config;
        private BitcornContext _dbContext;
        private string _refreshToken;
        private string _twitchToken = "eh2021s7mbedwgs03s8012bvk4ly8p";


        public Kraken(IConfiguration configuration, BitcornContext dbContext)
        {
            _config = configuration;
            _dbContext = dbContext;
            _refreshToken = _config.GetSection("Config").GetSection("TwitchSubOAuthRefreshToken").Value;
        }



        public async Task Nachos()
        {
            var subs = new List<Sub>();
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
                    subs.Add(new Sub()
                    {
                        Tier = subscription.sub_plan,
                        TwitchId = subscription.user._id
                    });
                }
                total = data._total;
                offset += 100;
            }

            await UpdateSubTiers(subs.ToArray());

            var discordSubs = await _dbContext.UserIdentity.
                Where(u => u.DiscordId != null).
                Join(_dbContext.User,
                    identity => identity.UserId,
                    us => us.UserId,
                    (id, u) => new SubTierDiscord(id.DiscordId, u.SubTier)).
                ToArrayAsync();
            var client = new RestClient(@"https://b23d8cc0.ngrok.io/");

            //create request
            var req = new RestRequest(Method.GET);
            req.Resource = "discord";
            req.AddJsonBody(discordSubs);
            client.Execute(req);
        }

        public async Task<bool> UpdateSubTiers(Sub[] subs)
        {
            try
            {
                var t1 = subs.Where(s => s.Tier == "1000").ToList();
                var t2 = subs.Where(s => s.Tier == "2000").ToList();
                var t3 = subs.Where(s => s.Tier == "3000").ToList();


                await _dbContext.Database.ExecuteSqlRawAsync("UPDATE [user] SET [subtier] = 0");
                if (t1.Count > 0)
                    await _dbContext.Database.ExecuteSqlRawAsync(BitcornUtils.BuildSubtierUpdateString(t1, 1));
                if (t2.Count > 0)
                    await _dbContext.Database.ExecuteSqlRawAsync(BitcornUtils.BuildSubtierUpdateString(t2, 2));
                if (t3.Count > 0)
                    await _dbContext.Database.ExecuteSqlRawAsync(BitcornUtils.BuildSubtierUpdateString(t3, 3));

                return true;
            }
            catch (Exception e)
            {
                //_logger.LogError(e, $"Failed to update subtiers {data}");
                return false;
            }
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
