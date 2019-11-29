using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserWalletController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public UserWalletController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost]
        public async Task<UserWallet> Wallet([FromBody] UserIdBody userIdBody)
        {
            var platformId = BitcornUtils.GetPlatformId(userIdBody.Id);
            var userIdentity = BitcornUtils.GetUserIdentityForPlatform(platformId, _dbContext);
            var userWallet = await _dbContext.UserWallet.FirstOrDefaultAsync(w => w.UserId == userIdentity.Id);
            return userWallet;
        }
    }
}
