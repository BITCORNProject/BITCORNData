using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Utils.Wallet;
using BITCORNService.Utils.Wallet.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {

        //API: /api/wallet/createcornaddy
        [HttpPost("CreateCornaddy")]
        public async Task<object> CreateCornaddy([FromBody] dynamic input)
        {
            //TODO: select user
            //TODO: select wallet server
            string endpoint = GetWalletServerEndpoint();
            string accessToken = await GetWalletServerAccessToken();

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
        public async Task<object> Deposit([FromBody] dynamic input)
        { 
            int? index = input.index?.Value;
            if (index == null)
            {
                throw new ArgumentException("index");
            }
            string block = input.block?.Value;

            if (string.IsNullOrWhiteSpace(block))
            {
                throw new ArgumentException("block");
            }

            JArray payments = input?.payments;

            if (payments == null)
            {
                throw new ArgumentException("payments");
            }

            //TODO: update user balance
            throw new NotImplementedException();
        }

        //API: /api/wallet/withdraw
        [HttpPost("Withdraw")]
        public async Task<object> Withdraw([FromBody] dynamic input)
        {
            decimal amount = 0;
            if (input.amount.Value != null)
            {
                amount = Convert.ToDecimal(input.amount.Value);
            }
            else
            {
                throw new ArgumentException("amount");
            }

            string withdrawalAddress = input.cornaddy?.Value;

            if (string.IsNullOrWhiteSpace(withdrawalAddress))
            {
                throw new ArgumentException("cornaddy");
            }

            //TODO: select user
            string endpoint = GetWalletServerEndpoint();
            string accessToken = await GetWalletServerAccessToken();

            using (var client = new WalletClient(endpoint, accessToken))
            {
                var response = await client.SendFromAsync("main", withdrawalAddress, amount, 120);
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

        //TODO: implement access token fetching
        private async Task<string> GetWalletServerAccessToken()
        {
            throw new NotImplementedException();
        }
        //TODO: implement server fetching
        private string GetWalletServerEndpoint()
        {
            throw new NotImplementedException();
        }

    }
}