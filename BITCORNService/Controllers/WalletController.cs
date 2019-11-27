using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.Wallet;
using BITCORNService.Utils.Wallet.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        IConfiguration _configuration;
        
        public WalletController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        //API: /api/wallet/createcornaddy
        [HttpPost("CreateCornaddy")]
        public async Task<object> CreateCornaddy([FromBody] WalletCreateCornaddyRequest request)
        {
            //TODO: select user
            //TODO: select wallet server
            string accessToken = await GetWalletServerAccessToken();
            string endpoint = GetWalletServerEndpoint();

            using (var client = new WalletClient(endpoint, accessToken))
            {
                var response = await client.GetNewAddressAsync("main");
                if (!response.IsError)
                {
                    var address = response.GetParsedContent();
                    //TODO: assign address to the user
                }
                //we got an error, fetch the internal wallet error code and figure out what to do
                else
                {
                    //get wallet error response
                    var error = response.GetError();
                   
                    if (error.Code == WalletErrorCodes.HTTP_ERROR)
                    {
                        //TODO: figure out what to do when wallet server is not reached
                    }
                }
            }
            throw new NotImplementedException();
        }

        //API: /api/wallet/deposit
        //called by the wallet servers only
        [HttpPost("Deposit")]
        public async Task<object> Deposit([FromBody] WalletDepositRequest request)
        { 
            //TODO: update user balance
            throw new NotImplementedException();
        }

        //API: /api/wallet/withdraw
        [HttpPost("Withdraw")]
        public async Task<object> Withdraw([FromBody] WalletWithdrawalRequest request)
        {
            //TODO: select user
            string endpoint = GetWalletServerEndpoint();
            string accessToken = await GetWalletServerAccessToken();

            using (var client = new WalletClient(endpoint, accessToken))
            {
                var response = await client.SendFromAsync("main", request.Cornaddy, request.Amount, 120);
                if (!response.IsError)
                {
                    //TODO: subract balance from user
                }
                //we got an error, fetch the internal wallet error code and figure out what to do
                else
                {
                    //get wallet error response
                    var error = response.GetError();
                 
                    //invalid withdrawal address
                    if (error.Code == WalletErrorCodes.RPC_INVALID_ADDRESS_OR_KEY)
                    {
                        //TODO: figure out what to do when withdrawal address is not a cornaddy
                    }
                    //too much immature corn to complete this transaction at this time
                    else if(error.Code == WalletErrorCodes.RPC_WALLET_INSUFFICIENT_FUNDS)
                    {
                        //TODO: figure out what to do
                    }
                    //wallet server was not reached
                    else if(error.Code == WalletErrorCodes.HTTP_ERROR)
                    {
                        //TODO: figure out what to do when wallet server is not reached
                    }

                }
            }

            throw new NotImplementedException();
        }

        private async Task<string> GetWalletServerAccessToken()
        {
            string endpoint = _configuration["Config:WalletServerTokenEndpoint"];

            var requestBody = JsonConvert.SerializeObject(new {
                client_id = _configuration["Config:WalletServerClientId"],
                client_secret = _configuration["Config:WalletServerClientSecret"],
                audience = _configuration["Config:WalletServerAudience"],
                grant_type = _configuration["Config:WalletServerGrantType"]
            });

            var client = new RestClient(endpoint);
            var request = new RestRequest(Method.POST);

            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", requestBody, ParameterType.RequestBody);
         
            var response = await client.ExecuteTaskAsync(request, new CancellationTokenSource().Token);

            return JObject.Parse(response.Content)["access_token"].ToString();
        }
        //TODO: implement server fetching
        private string GetWalletServerEndpoint()
        {
            throw new NotImplementedException();
        }

    }
}