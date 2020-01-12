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
using System.Text;
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
                    cornResponse.WalletObject = address;
                    await dbContext.SaveAsync();
                    
                }
                //we got an error, fetch the internal wallet error code and figure out what to do
                else
                {
                    //get wallet error response
                    //var error = response.GetError();
                    cornResponse.WalletAvailable = false;
                }
            }
        }

        public static async Task<BitcornResponse> CreateCornaddy(BitcornContext dbContext, UserWallet userWallet, IConfiguration configuration)
        {
            var cornResponse = new BitcornResponse();
            cornResponse.WalletAvailable = true;
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
                        cornResponse.WalletAvailable = false;
                        return cornResponse;
                    }
                }

                string accessToken = await GetWalletServerAccessToken(configuration);
                //failed to fetch access token
                if (!CheckAccessTokenExists(accessToken))
                {
                    throw new UnauthorizedAccessException("Failed to fetch wallet server access token");
                }
                await CreateCornaddyInternal(cornResponse, dbContext, server, userWallet, accessToken);
            }
            catch (Exception e)
            {
                throw e;
            }
            return cornResponse;
        }
        static bool CheckAccessTokenExists(string accessToken)
        {
            return !string.IsNullOrEmpty(accessToken);
        }
        public static async Task<CornTx> DebitWithdrawTx(string txId, User user, WalletServer server, decimal amount, BitcornContext dbContext, string platform)
        {
            if (user.UserWallet.Balance >= amount)
            {
                var sql = new StringBuilder();
                sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), amount, '-', nameof(UserWallet.UserId), user.UserId));
                sql.Append(TxUtils.ModifyNumber(nameof(WalletServer), nameof(WalletServer.ServerBalance), amount, '-', nameof(WalletServer.Id), server.Id));
                await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());

                var log = new CornTx();
                log.BlockchainTxId = txId;
                log.Amount = amount;
                log.Timestamp = DateTime.Now;
                log.TxType = "$withdraw";
                log.TxGroupId = Guid.NewGuid().ToString();
                log.Platform = platform;
                log.ReceiverId = user.UserId;
                dbContext.CornTx.Add(log);
                await dbContext.SaveAsync();
                return log;
            }
            return null;
        }
        public static async Task<BitcornResponse> Withdraw(BitcornContext dbContext, IConfiguration configuration,User user,string cornAddy,decimal amount,string platform)
        {
            var cornResponse = new BitcornResponse();
            cornResponse.WalletAvailable = true;
            try
            {
             
                if (user.IsBanned)
                {
                    return cornResponse;
                }
                if (user.UserWallet.Balance < amount)
                {
                    return cornResponse;
                }

                var server = await dbContext.GetWalletServer(user.UserWallet);
                if (!server.Enabled || !server.WithdrawEnabled)
                {
                    cornResponse.WalletAvailable = false;
                    return cornResponse;
                }

                string accessToken = await GetWalletServerAccessToken(configuration);
                //failed to fetch access token
                if (!CheckAccessTokenExists(accessToken))
                {
                    throw new UnauthorizedAccessException("Failed to fetch wallet server access token");
                }

                using (var client = new WalletClient(server.Endpoint, accessToken))
                {
                    var response = await client.SendToAddressAsync(cornAddy, amount);
                    if (!response.IsError)
                    {
                        string txId = response.GetParsedContent();
                        await DebitWithdrawTx(txId,user,server,amount,dbContext,platform);
                        cornResponse.WalletObject = txId;

                    }
                    //we got an error, fetch the internal wallet error code and figure out what to do
                    else
                    {
                        //get wallet error response
                        var error = response.GetError();

                        //invalid withdrawal address
                        if (error.Code == WalletErrorCodes.RPC_INVALID_ADDRESS_OR_KEY)
                        {
                            cornResponse.UserError = true;
                        }
                        //too much immature corn to complete this transaction at this time
                        else if (error.Code == WalletErrorCodes.RPC_WALLET_INSUFFICIENT_FUNDS)
                        {
                            cornResponse.WalletAvailable = false;
                        }
                        //wallet server was not reached
                        else if (error.Code == WalletErrorCodes.HTTP_ERROR)
                        {
                            cornResponse.WalletAvailable = false;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                throw e;
            }

            return cornResponse;
        }
                
        public static async Task<CornTx[]> Deposit(BitcornContext dbContext, WalletDepositRequest request)
        {
            var receipts = new List<CornTx>();
            try
            {
                var server = dbContext.WalletServer.FirstOrDefault((s) => s.Index == request.Index);
                int newDeposits = 0;

                var sql = new StringBuilder();
                foreach (var payment in request.Payments)
                {
                    decimal amount = payment.Amount;
                    string address = payment.Address;
                    string txid = payment.TxId;

                    bool isLogged = await dbContext.IsDepositRegistered(txid);

                    if (!isLogged)
                    {
                        newDeposits++;
                        var wallet = await dbContext.WalletByAddress(address);
                        if (wallet != null)
                        {
                            var cornTx = new CornTx();
                            cornTx.Amount = amount;
                            cornTx.BlockchainTxId = txid;
                            cornTx.ReceiverId = wallet.UserId;
                            cornTx.SenderId = null;
                            cornTx.Timestamp = DateTime.Now;
                            cornTx.TxType = TransactionType.receive.ToString();
                            cornTx.Platform = "wallet-server";
                            cornTx.TxGroupId = Guid.NewGuid().ToString();
                            var deposit = new CornDeposit();
                            deposit.TxId = txid;
                            deposit.UserId = wallet.UserId;

                            sql.Append(TxUtils.ModifyNumber(nameof(UserWallet), nameof(UserWallet.Balance), amount, '+', nameof(UserWallet.UserId), wallet.UserId));
                            sql.Append(TxUtils.ModifyNumber(nameof(WalletServer), nameof(WalletServer.ServerBalance), amount, '+', nameof(WalletServer.Id), server.Id));
                           
                            dbContext.CornTx.Add(cornTx);
                            dbContext.CornDeposit.Add(deposit);
                            receipts.Add(cornTx);
                        }
                    }
                }

                if (newDeposits > 0)
                {
                    server.LastBalanceUpdateBlock = request.Block;
                    int count = await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                    await dbContext.SaveAsync();
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(dbContext, e);
            }
            return receipts.ToArray();
        }
    }
}
