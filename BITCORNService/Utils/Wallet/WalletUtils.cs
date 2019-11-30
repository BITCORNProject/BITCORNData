using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Wallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Wallet
{
    public static class WalletUtils
    {
        /// <summary>
        /// This method is used to get the index of the wallet server
        /// that new users should get a new BITCORN address on.
        /// Servers will be assigned to new users when they register for a
        /// wallet address in a round robin fashion.
        /// </summary>
        /// <returns>(int) The index of the wallet server to create the new address on</returns>
        public static async Task<int> GetWalletIndexAsync(BitcornContext dbContext)
        {
            var count = dbContext.WalletServer.Count();
            var currentIndex = dbContext.WalletIndex.FirstOrDefault(x => x.Id == 1);
            if (currentIndex != null)
            {
                currentIndex.Index = (currentIndex.Index + 1) % count;
                // TODO: proper logging
                await dbContext.SaveAsync();
                return currentIndex.Index.Value;
            }
            return 1;
        
        }
        public static async Task CreateCornaddy(BitcornContext dbContext, UserIdentity userIdentity, IConfiguration configuration)
        {
            var index = await GetWalletIndexAsync(dbContext);
            var server = await dbContext.WalletServer.FirstOrDefaultAsync(w => w.Index == index);
            if (!server.Enabled)
            {
                //TODO: what to do when server is not enabled, maybe reroll index?
            }

            string accessToken = await GetWalletServerAccessToken(configuration);
            using (var client = new WalletClient(server.Endpoint, accessToken))
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
        public static async Task<string> GetWalletServerAccessToken(IConfiguration configuration)
        {

            string endpoint = configuration["Config:WalletServerTokenEndpoint"];

            var requestBody = JsonConvert.SerializeObject(new
            {
                client_id = configuration["Config:WalletServerClientId"],
                client_secret = configuration["Config:WalletServerClientSecret"],
                audience = configuration["Config:WalletServerAudience"],
                grant_type = configuration["Config:WalletServerGrantType"]
            });

            var client = new RestClient(endpoint);
            var request = new RestRequest(Method.POST);

            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", requestBody, ParameterType.RequestBody);
            using (var cancellationToken = new CancellationTokenSource())
            {
                var response = await client.ExecuteTaskAsync(request, cancellationToken.Token);

                return JObject.Parse(response.Content)["access_token"].ToString();
            }
        }
        //TODO: check if there is better way to access config
        public static async Task Withdraw(BitcornContext dbContext, IConfiguration configuration, string cornaddy, decimal amount, UserWallet userWallet)
        {
            var server = await dbContext.WalletServer.FirstOrDefaultAsync(w => w.Index == userWallet.WalletServer.Value);
            if (!server.Enabled)
            {
                //TODO: figure out what to do when server has been disabled
            }

            string accessToken = await GetWalletServerAccessToken(configuration);

            using (var client = new WalletClient(server.Endpoint, accessToken))
            {
                var response = await client.SendFromAsync("main", cornaddy, amount, 120);
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
                    else if (error.Code == WalletErrorCodes.RPC_WALLET_INSUFFICIENT_FUNDS)
                    {
                        //TODO: figure out what to do
                    }
                    //wallet server was not reached
                    else if (error.Code == WalletErrorCodes.HTTP_ERROR)
                    {
                        //TODO: figure out what to do when wallet server is not reached
                    }

                }
            }

            throw new NotImplementedException();
        }
        //TODO: need better way to access config
        public static async Task Withdraw(BitcornContext dbContext, IConfiguration configuration, string cornaddy, decimal amount, UserIdentity user)
        {
            var userWallet = await dbContext.UserWallet.FirstOrDefaultAsync(w => w.UserId == user.UserId);
            await Withdraw(dbContext,configuration,cornaddy,amount,userWallet);
        }
        public static async Task Deposit(BitcornContext dbContext, WalletDepositRequest request)
        {
            try
            {
                var server = dbContext.WalletServer.FirstOrDefault((s) => s.Index == request.Index);
                int newDeposits = 0;
                foreach (dynamic payment in request.Payments)
                {
                    decimal amount = payment.amount;
                    string address = payment.address;
                    string txid = payment.txid;

                    bool isLogged = await dbContext.IsBlockchainTransactionLogged(txid);

                    if (!isLogged)
                    {
                        newDeposits++;
                        var wallet = await dbContext.WalletByAddress(address);
                        wallet.Balance += amount;

                        var cornTx = new CornTx();
                        cornTx.Amount = amount;
                        cornTx.BlockchainTxId = txid;
                        //TODO: why is this a string?
                        cornTx.ReceiverId = wallet.UserId.ToString();
                        //TODO: this field must not be required
                        cornTx.SenderId = null;
                        cornTx.Timestamp = DateTime.Now;
                        cornTx.TxType = TransactionType.receive.ToString();
                        cornTx.Platform = "wallet-server";

                        dbContext.CornTx.Add(cornTx);

                    }
                }

                if (newDeposits > 0)
                {
                    server.LastBalanceUpdateBlock = request.Block;
                    await dbContext.SaveAsync();
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(e);
            }
        }
    }
}
