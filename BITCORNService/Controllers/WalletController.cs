using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Wallet;
using BITCORNService.Utils.Wallet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Models;

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
        //API: /api/wallet/createcornaddy
        [HttpPost("CreateCornaddy")]
        public async Task<object> CreateCornaddy([FromBody] WalletCreateCornaddyRequest request)
        {
            //TODO: this needs user fetching
            //WalletUtils.CreateCornaddy(_dbContext,request);
            throw new NotImplementedException();
        }

        //API: /api/wallet/deposit
        //called by the wallet servers only
        [HttpPost("Deposit")]
        public async Task<ActionResult> Deposit([FromBody] WalletDepositRequest request)
        {
            await WalletUtils.Deposit(_dbContext, request);

            return Ok();
        }
        

    }
}