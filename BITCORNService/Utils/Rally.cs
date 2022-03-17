using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BITCORNService.Utils
{
    public class Rally
    {
        IConfiguration _configuration;
        RestClient client;
        public Rally(IConfiguration config)
        {
            _configuration = config;
            var api = _configuration["Config:RallyDataAPI"];
         //   api = "http://localhost:56144/api";
            client = new RestClient(api);
        }

        public void CreateUser(string rallyId, string rallyUsername)
        {
            var request = new RestRequest($"user/create/{rallyId}/${rallyUsername}");
            client.Get(request);

        }

        public void SyncYoutube(string rallyId, string youtubeId, string youtubeUsername, string youtubeRefreshToken)
        {
            var request = new RestRequest($"user/{rallyId}/sync/youtube");
            var body = new
            {
                youtubeId,
                youtubeUsername,
                youtubeRefreshToken
            };
            request.AddJsonBody(body);
            var response = client.Post(request);


        }

       

        public class RallyUser
        {
          
            public long UserId { get; set; }
            public DateTime CreationTime { get; set; }
            public string SessionToken { get; set; }
            public DateTime? SessionTokenExpires { get; set; }
            public bool IsBanned { get; set; }
   
            public  RallyUserWallet[] UserWallet { get; set; }
        }

        public RallyUserWallet[] GetWallets(string rallyId)
        {
            try
            {
                var request = new RestRequest($"user/lookup/rally|{rallyId}?wallets=all");

                var response = client.Get(request);
                return JsonConvert.DeserializeObject<RallyUser>(response.Content).UserWallet;
            }
            catch
            {
                return new RallyUserWallet[] { };
            }
        }
    }

    public class RallyUserWallet
    {
        public long UserWalletId { get; set; }
        public long UserId { get; set; }

        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public string Token { get; set; }

        //public User User { get; set; }
    }
}
