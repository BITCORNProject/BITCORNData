using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
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
        public async Task<ActionResult<UserWallet>> CreateCornaddy([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            
            var platformId = BitcornUtils.GetPlatformId(id);

            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            
            var response = await WalletUtils.CreateCornaddy(_dbContext, user.UserWallet, _configuration);

            if (response.HttpCode == HttpStatusCode.OK)
            {
                return user.UserWallet;
            }
            else
            {
                return StatusCode((int)response.HttpCode);
            }
            //WalletUtils.CreateCornaddy(_dbContext,request);

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