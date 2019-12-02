using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using BITCORNService.Utils.Wallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private static async Task<UserWallet> GetUserWallet(this BitcornContext dbContext, WithdrawUser withdrawUser)
        {
            var platformId = BitcornUtils.GetPlatformId(withdrawUser.Id);
            var identity = await BitcornUtils.GetUserIdentityForPlatform(platformId,dbContext);
            return await dbContext.GetUserWallet(identity);
        }
        public static async Task<UserWallet> GetUserWallet(this BitcornContext dbContext, UserIdentity identity)
        {
            return await dbContext.UserWallet.FirstOrDefaultAsync(w => w.UserId == identity.UserId);
        }
        static async Task HandleException(BitcornResponse response,Exception e)
        {
            var errorLog = await BITCORNLogger.LogError(e);
            response.Message = $"Something went wrong; error id: {errorLog.Id}";
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
                try
                {
                    var response = await client.ExecuteTaskAsync(request, cancellationToken.Token);

                    return JObject.Parse(response.Content)["access_token"].ToString();
                }
                catch
                {
                    return null;
                }
            }
        }
        public static async Task<WalletServer> GetWalletServer(this BitcornContext dbContext, UserWallet userWallet)
        {
            return await dbContext.GetWalletServer(userWallet.WalletServer.Value);
        }
        public static async Task<WalletServer> GetWalletServer(this BitcornContext dbContext, int index)
        {
            return await dbContext.WalletServer.FirstOrDefaultAsync(w => w.Index == index);
        }
        static void OnWalletServerNotAvailable(BitcornResponse cornResponse)
        {
            cornResponse.HttpCode = HttpStatusCode.ServiceUnavailable;
            cornResponse.Message = "No wallet available to process this request.";
        }
        /// <summary>
        /// wallet was marked as enabled, but it was not reached, this is never intended
        /// </summary>
        static async Task OnWalletNotReached(string method,BitcornResponse cornResponse, WalletError error)
        {
            cornResponse.HttpCode = HttpStatusCode.ServiceUnavailable;
            var e = new WebException($"({method}) Failed to reach wallet server: ({error.Code.ToString()}), {error.Message}");

            await HandleException(cornResponse, e);
        }
        static async Task CreateCornaddyInternal(BitcornResponse cornResponse, BitcornContext dbContext, WalletServer walletServer, UserWallet userWallet, string accessToken)
        {

            using (var client = new WalletClient(walletServer.Endpoint, accessToken))
            {
                var response = await client.GetNewAddressAsync("main");
                if (!response.IsError)
                {
                    var address = response.GetParsedContent();
                    userWallet.CornAddy = address;
                    userWallet.WalletServer = walletServer.Index;
                    try
                    {
                        await dbContext.SaveAsync();
                        cornResponse.HttpCode = HttpStatusCode.OK;
                    }
                    catch
                    {
                        cornResponse.HttpCode = HttpStatusCode.InternalServerError;
                        cornResponse.Message = "Failed to save changes to the database.";
                    }
                }
                //we got an error, fetch the internal wallet error code and figure out what to do
                else
                {
                    //get wallet error response
                    var error = response.GetError();
                    cornResponse.Message = error.Message;
                    if (error.Code == WalletErrorCodes.HTTP_ERROR)
                    {
                        await OnWalletNotReached("CreateCornaddy",cornResponse, error);
                    }
                    else
                    {
                        //this should not be happening
                        cornResponse.HttpCode = HttpStatusCode.InternalServerError;
                        var e = new Exception($"(CreateCornaddy) Unhandled wallet response: ({error.Code.ToString()}), {error.Message}");

                        await HandleException(cornResponse, e);
                    }
                }
            }
        }

        public static async Task<BitcornResponse> CreateCornaddy(BitcornContext dbContext, UserIdentity userIdentity, IConfiguration configuration)
        {
            var cornResponse = new BitcornResponse();
            try
            {
                var index = await GetWalletIndexAsync(dbContext);

                var server = await dbContext.GetWalletServer(index);

                //wallet server has been disabled, find the first server that has been enabled
                if (!server.Enabled)
                {
                    server = await dbContext.WalletServer.FirstOrDefaultAsync(s => s.Enabled);
                    //server is still null, return
                    if (server == null)
                    {
                        OnWalletServerNotAvailable(cornResponse);
                        return cornResponse;
                    }
                }

                var userWallet = await dbContext.GetUserWallet(userIdentity);
                string accessToken = await GetWalletServerAccessToken(configuration);
                //failed to fetch access token
                if (!CheckAccessTokenExists(accessToken))
                {
                    cornResponse.HttpCode = HttpStatusCode.Unauthorized;
                    var e = new UnauthorizedAccessException("Failed to fetch wallet server access token");
                    await HandleException(cornResponse, e);
                    return cornResponse;
                }
                await CreateCornaddyInternal(cornResponse, dbContext, server, userWallet, accessToken);
            }
            catch (Exception e)
            {
                await HandleException(cornResponse, e);
                throw e;
            }
            return cornResponse;
        }
        static bool CheckAccessTokenExists(string accessToken)
        {
            return !string.IsNullOrEmpty(accessToken);
        }
        
        public static async Task<BitcornResponse> Withdraw(BitcornContext dbContext, IConfiguration configuration, WithdrawUser withdrawUser)
        {
            var cornResponse = new BitcornResponse();
            try
            {
                var userWallet = await GetUserWallet(dbContext, withdrawUser);
                if (userWallet.Balance < withdrawUser.Amount)
                {
                    cornResponse.HttpCode = HttpStatusCode.PaymentRequired;
                    return cornResponse;
                }

                var server = await dbContext.GetWalletServer(userWallet);
                if (!server.Enabled || !server.WithdrawEnabled)
                {
                    OnWalletServerNotAvailable(cornResponse);
                    return cornResponse;
                }

                string accessToken = await GetWalletServerAccessToken(configuration);
                //failed to fetch access token
                if (!CheckAccessTokenExists(accessToken))
                {
                    cornResponse.HttpCode = HttpStatusCode.Unauthorized;
                    var e = new UnauthorizedAccessException("Failed to fetch wallet server access token");
                    await HandleException(cornResponse, e);
                    return cornResponse;
                }

                using (var client = new WalletClient(server.Endpoint, accessToken))
                {
                    var response = await client.SendFromAsync("main", withdrawUser.CornAddy, withdrawUser.Amount, 120);
                    if (!response.IsError)
                    {
                        try
                        {
                            string txId = response.GetParsedContent();
                            //TODO: log txid
                            await TxUtils.ExecuteDebitTx(withdrawUser, dbContext);
                            cornResponse.HttpCode = HttpStatusCode.OK;
                            cornResponse.Message = txId;
                        }
                        catch
                        {
                            cornResponse.HttpCode = HttpStatusCode.InternalServerError;
                            cornResponse.Message = "Failed to save changes to the database.";
                        }
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
                            cornResponse.HttpCode = HttpStatusCode.BadRequest;
                            var e = new Exception($"Invalid Corn address {withdrawUser.CornAddy} Wallet error: ({error.Code.ToString()}), {error.Message}");

                            await HandleException(cornResponse, e);
                        }
                        //too much immature corn to complete this transaction at this time
                        else if (error.Code == WalletErrorCodes.RPC_WALLET_INSUFFICIENT_FUNDS)
                        {
                            cornResponse.HttpCode = HttpStatusCode.PreconditionFailed;
                            var e = new Exception($"Not enough mature corn to complete this withdrawal, Wallet error: ({error.Code.ToString()}), {error.Message}");

                            await HandleException(cornResponse, e);
                        }
                        //wallet server was not reached
                        else if (error.Code == WalletErrorCodes.HTTP_ERROR)
                        {
                            await OnWalletNotReached("Withdraw", cornResponse, error);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                await HandleException(cornResponse,e);
                throw e;
            }

            return cornResponse;
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
