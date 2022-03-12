using BITCORNService.Models;
using BITCORNService.Utils.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Twitch
{
    public class Helix
    {
        private IConfiguration _config;
        private BitcornContext _dbContext;
        string _accessToken = null;
        public Helix(IConfiguration configuration, BitcornContext dbContext, string token)
        {
            _config = configuration;
            _dbContext = dbContext;
            _accessToken = token;
        }
        public async Task<Dictionary<string, string>> GetSubs(string twitchId)
        {
            HelixSubResponse initial = null;
            try
            {
                var userIds = new Dictionary<string, string>();
                initial = await SubscriptionsRequest(twitchId, null);
                if (initial != null)
                {
                    //if (initial.data == null) return 0;
                    string pagination = null;
                    if (initial.pagination != null && !string.IsNullOrEmpty(initial.pagination.cursor))
                    {
                        pagination = initial.pagination.cursor;
                    }

                    for (int i = 0; i < initial.data.Length; i++)
                    {
                        var userId = initial.data[i].user_id;
                        if (!userIds.ContainsKey(userId))
                        {
                            userIds.Add(initial.data[i].user_id, initial.data[i].tier);
                        }
                    }

                    while (!string.IsNullOrEmpty(pagination))
                    {
                        if (!string.IsNullOrEmpty(pagination))
                        {
                            var output = await SubscriptionsRequest(twitchId, pagination);
                            if (output != null && output.data != null)
                            {
                                for (int i = 0; i < output.data.Length; i++)
                                {
                                    var userId = output.data[i].user_id;
                                    if (!userIds.ContainsKey(userId))
                                    {
                                        userIds.Add(userId, output.data[i].tier);
                                    }
                                }

                                if (output.pagination != null)
                                {
                                    pagination = output.pagination.cursor;
                                }
                            }
                        }
                    }
                }

                return userIds;
            }
            catch (Exception ex)
            {
                await BITCORNLogger.LogError(_dbContext, ex, JsonConvert.SerializeObject(initial));
                throw ex;
                //return new Dictionary<string, string>();
            }
        }

        public static string GetAccessToken(IConfiguration configuration)
        {
         
            var url = "https://id.twitch.tv/";
            var body = new
            {
                client_id = configuration.GetSection("Config").GetSection("TwitchClientIdSub").Value,
                client_secret = configuration.GetSection("Config").GetSection("TwitchClientSecretSub").Value,
                grant_type = "client_credentials"
            };

            var restClient = new RestClient(url);
            var request = new RestRequest("oauth2/token");
            request.AddParameter("client_id", body.client_id);

            request.AddParameter("client_secret", body.client_secret);
            request.AddParameter("grant_type", body.grant_type);

            var resp = restClient.Post(request);
            return JObject.Parse(resp.Content)["access_token"].ToString();
        }

        public async Task<TwitchUser> GetTwitchUser(string id)
        {
            var restClient = new RestClient(@"https://api.twitch.tv/");
            var resource = $"helix/users?id={id}";
            var request = new RestRequest(resource, DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            request.AddHeader("Client-ID", _config.GetSection("Config").GetSection("TwitchClientIdSub").Value);
            var response = restClient.Execute(request);
            var json = JObject.Parse(response.Content)["data"];
       
            var user = new TwitchUser();
            
            user.name = json[0]["login"].ToString();
            user._id = json[0]["id"].ToString();
            user.created_at = ((DateTime)json[0]["created_at"]);
            user.bio = json[0]["description"].ToString();
            user.display_name = json[0]["display_name"].ToString();
            user.logo = json[0]["profile_image_url"].ToString();
            return user;

        }

        public async Task<HelixSubResponse> SubscriptionsRequest(string twitchId, string pagination)
        {
            var restClient = new RestClient(@"https://api.twitch.tv/");
            var resource = $"helix/subscriptions?broadcaster_id={twitchId}&first=100";
            if (!string.IsNullOrEmpty(pagination))
            {
                resource += $"&after={pagination}";
            }

            var request = new RestRequest(resource, DataFormat.Json);
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            request.AddHeader("Client-ID", _config.GetSection("Config").GetSection("TwitchClientIdSub").Value);
            var response = restClient.Execute(request);
            var output = JsonConvert.DeserializeObject<HelixSubResponse>(response.Content);
            output.raw = response.Content;
            output.status = (int)response.StatusCode;
            return output;
            //return await restClient.GetAsync<HelixSubResponse>(request);

        }
    }
    public class HelixSubResponse
    {
        public Sub[] data { get; set; }
        public Pagination pagination { get; set; }
        public int status;
        public string raw;
        public HelixSubResponse()
        {

        }
        public class Pagination
        {
            public string cursor { get; set; }
            public Pagination()
            {

            }
        }
        public class Sub
        {
            public Sub()
            {

            }
            public string broadcaster_id { get; set; }
            public string broadcaster_login { get; set; }
            public string broadcaster_name { get; set; }
            public bool is_gift { get; set; }
            public string tier { get; set; }
            public string plan_name { get; set; }
            public string user_id { get; set; }
            public string user_login { get; set; }
            public string user_name { get; set; }
        }
    }
}
