using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Wallet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BITCORNService.Controllers
{
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
        [HttpPost("CreateCornaddy")]
        public async Task<object> CreateCornaddy([FromBody] CreateCornaddyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id)) throw new ArgumentNullException("id");
            
            var platformId = BitcornUtils.GetPlatformId(request.Id);

            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            var response = new Dictionary<string, object>();
            if (user != null)
            {
                var walletResponse = await WalletUtils.CreateCornaddy(_dbContext, user.UserWallet, _configuration);
                response.Add("UserError", walletResponse.UserError);
                response.Add("WalletAvailable",walletResponse.WalletAvailable);
                response.Add("CornAddy",walletResponse.WalletObject);
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
            return response;

        }

        //API: /api/wallet/deposit
        //called by the wallet servers only
        [HttpPost("deposit")]
        public async Task<ActionResult> Deposit([FromBody] WalletDepositRequest request)
        {
            await WalletUtils.Deposit(_dbContext, request);

            return Ok();
        }

        //API: /api/wallet/withdraw
        [HttpPost("withdraw")]
        public async Task<object> Withdraw([FromBody] WithdrawRequest request)
        {
            var platformId = BitcornUtils.GetPlatformId(request.Id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            var response = new Dictionary<string, object>();
            if (user != null)
            {
                var withdrawResult = await WalletUtils.Withdraw(_dbContext, _configuration,user,request.CornAddy,request.Amount, platformId.Platform);
                response.Add("UserError",withdrawResult.UserError);
                response.Add("WalletAvailable",withdrawResult.WalletAvailable);
                response.Add("TxId",withdrawResult.WalletObject);
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
            return response;
        
        }
    }
}