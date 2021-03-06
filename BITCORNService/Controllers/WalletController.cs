using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using BITCORNService.Utils.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly BitcornContext _dbContext;

        public WalletController(IConfiguration configuration, BitcornContext dbContext)
        {
            this._configuration = configuration;
            this._dbContext = dbContext;
        }
        //API: /api/wallet/createcornaddy/{id}
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("CreateCornaddy")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<FullUser>> CreateCornaddy([FromBody] CreateCornaddyRequest request)
        {
            try
            {
                if (this.GetCachedUser() != null)
                    throw new InvalidOperationException();
                if (string.IsNullOrWhiteSpace(request.Id)) throw new ArgumentNullException("id");

                var platformId = BitcornUtils.GetPlatformId(request.Id);

                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();

                if (user != null)
                {
                    var walletResponse = await WalletUtils.CreateCornaddy(_dbContext, user.UserWallet, _configuration);
                    if (!walletResponse.WalletAvailable)
                    {
                        user.UserWallet.CornAddy = "<no enabled wallets found>";
                        await _dbContext.SaveAsync();
                        return StatusCode(200);
                        //return StatusCode((int)HttpStatusCode.ServiceUnavailable);
                    }
                    return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
                }
                else
                {
                    return StatusCode(500);
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(request));
                throw e;
            }
        }
        //API: /api/wallet/server
        //called by the wallet servers only
        [HttpGet("server/{index}")]
        public async Task<WalletServer> Server([FromRoute] int index)
        {
            return await _dbContext.WalletServer.FirstOrDefaultAsync((s) => s.Index == index);
        }

        [HttpPost("lockall")]
        public async Task<ActionResult> LockAll()
        {
            var servers = await _dbContext.WalletServer.ToArrayAsync();
            for (int i = 0; i < servers.Length; i++)
            {
                servers[i].Enabled = false;
                servers[i].WithdrawEnabled = false;
            }
            await _dbContext.SaveAsync();
            return StatusCode(200);
        }


        [HttpGet("sold24")]
        public async Task<ActionResult<object>> GetSold24()
        {
            decimal sold24 = 0;
            try
            {
                sold24 = await TxUtils.GetSoldCorn24h(_dbContext);
            }
            catch
            {

            }

            return new 
            {
                sold24,
                available = Math.Abs(TxController.SELL_CORN_CAP_24H - sold24)
            };
        }

        [HttpGet("prices")]
        public async Task<ActionResult<object>> GetPrices()
        {
            var prices = await ProbitApi.GetPricesAsync(_dbContext);
            //(cornBtc, btcUsdt, cornPrice);
          
            return new
            {
                cornBtc = prices.Item1,
                btcUsdt = prices.Item2,
                cornPrice = prices.Item3,
            
            };
        }
        //API: /api/wallet/deposit
        //called by the wallet servers only
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("deposit")]
        [Authorize(Policy = AuthScopes.Deposit)]
        public async Task<ActionResult> Deposit([FromBody] WalletDepositRequest request)
        {
            try
            {
                if (this.GetCachedUser() != null)
                    throw new InvalidOperationException();
                var receipts = await WalletUtils.Deposit(_dbContext, request, _configuration);
                foreach (var receipt in receipts)
                {
                    if (receipt.Amount >= 100000)
                    {
                        var identity = await _dbContext.UserIdentity.FirstOrDefaultAsync(u => u.UserId == receipt.ReceiverId);
                        await BitcornUtils.TxTracking(_configuration, new
                        {
                            txid = receipt.BlockchainTxId,
                            time = DateTime.Now,
                            method = "deposit",
                            platform = "wallet-server",
                            amount = receipt.Amount,
                            userid = receipt.ReceiverId,
                            twitchUsername = identity.TwitchUsername,
                            discordUsername = identity.DiscordUsername
                        });
                    }
                }
                return Ok();
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(request));
                throw e;
            }

        }

        async Task FillColumns(Dictionary<string, object> response, WithdrawRequest request, User user)
        {
            if (request.Columns.Length > 0)
            {
                var columns = await UserReflection.GetColumns(_dbContext, request.Columns, new int[] { user.UserId });
                if (columns.Count > 0)
                {
                    foreach (var item in columns.First().Value)
                    {
                        response.Add(item.Key, item.Value);
                    }
                }
            }
        }

        //API: /api/wallet/withdraw
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("withdraw")]
        [Authorize(Policy = AuthScopes.Withdraw)]
        public async Task<ActionResult<object>> Withdraw([FromBody] WithdrawRequest request)
        {

            try
            {
                var platformId = BitcornUtils.GetPlatformId(request.Id);
                var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();

                var response = new Dictionary<string, object>();
                if (user != null)
                {
                    if (user.UserWallet.IsLocked != null && user.UserWallet.IsLocked.Value)
                    {
                        return StatusCode(420);
                    }

                    //if (user.MFA)
                    {
                        if (user.UserWallet.Balance < 30_000_000)
                        {


                            var withdrawResult = await WalletUtils.Withdraw(_dbContext, _configuration, user, request.CornAddy, request.Amount, platformId.Platform);
                            response.Add("usererror", withdrawResult.UserError);
                            response.Add("walletavailable", withdrawResult.WalletAvailable);
                            response.Add("txid", withdrawResult.WalletObject);

                            if (withdrawResult.ErrorCode != null && withdrawResult.ErrorCode == Utils.Wallet.Models.WalletErrorCodes.RPC_WALLET_INSUFFICIENT_FUNDS)
                            {
                                await BitcornUtils.TxTracking(_configuration, new
                                {
                                    message = "Insufficient funds detected on wallet on withdraw, (this might be because of no mature utxos) address to deposit from cold reserve  = " +
                                    withdrawResult.DepositAddress
                                });
                            }

                            await FillColumns(response, request, user);
                            if (withdrawResult.WalletObject != null && request.Amount > 100000)
                            {
                                await BitcornUtils.TxTracking(_configuration, new
                                {
                                    txid = withdrawResult.WalletObject,
                                    time = DateTime.Now,
                                    method = "withdraw",
                                    platform = platformId.Platform,
                                    amount = request.Amount,
                                    userid = user.UserId,
                                    twitchUsername = user.UserIdentity.TwitchUsername,
                                    discordUsername = user.UserIdentity.DiscordUsername,
                                    cornaddy = request.CornAddy,
                                });
                            }
                        }
                        else
                        {
                            response.Add("usererror", false);
                            response.Add("walletavailable", false);
                            response.Add("txid", null);
                            await FillColumns(response, request, user);
                            await BitcornUtils.TxTracking(_configuration, new
                            {

                                message = "Withdraw cancelled, manual withdraw required.",
                                time = DateTime.Now,

                                platform = platformId.Platform,
                                amount = request.Amount,
                                userid = user.UserId,
                                twitchUsername = user.UserIdentity.TwitchUsername,
                                discordUsername = user.UserIdentity.DiscordUsername,
                                cornaddy = request.CornAddy,
                            });
                        }
                    }
                    /*
                    else
                    {
                        await FillColumns(response, request, user);
                        response.Add("usererror", false);
                        response.Add("walletavailable", false);
                        response.Add("txid", null);
                    }*/
                }
                return response;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(request));
                throw e;
            }

        }
    }
}