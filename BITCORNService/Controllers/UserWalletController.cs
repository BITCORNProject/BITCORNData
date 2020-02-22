using System;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserWalletController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public UserWalletController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost("{id}")]
        public async Task<UserWallet> Wallet([FromRoute] string id)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            var userWallet = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).Select(u => u.UserWallet).FirstOrDefaultAsync();
            var referralId = _dbContext.UserReferral.FirstOrDefault(r => r.UserId == userWallet.UserId)?.ReferralId;
            if (referralId != 0 && referralId != null)
            {
                var userReferral = await _dbContext.UserReferral.FirstOrDefaultAsync(r => r.UserId == userWallet.UserId);
                var referrer = await _dbContext.Referrer.FirstOrDefaultAsync(r => r.ReferralId == userReferral.ReferralId);
                var referrerWallet = await _dbContext.UserWallet.FirstOrDefaultAsync(w => w.UserId == referrer.UserId);
                
                if(userReferral!= null
                   && userReferral.SignupReward 
                   && userReferral.MinimumBalanceDate != null
                   && userReferral.WalletDownloadDate != null
                   && userReferral.Bonus == false 
                   && userReferral.ReferrerBonus == false)
                {
                    //userWallet.Balance += referrer?.Amount;
                    referrerWallet.Balance += referrer?.Amount;
                    userReferral.Bonus = true;
                    userReferral.ReferrerBonus = true;
                }
                
                if (userReferral != null 
                    && userReferral.MinimumBalanceDate == null
                    && userReferral.WalletDownloadDate != null
                    && userWallet.Balance >= 1000 )
                {
                    userWallet.Balance += 100;
                    userReferral.MinimumBalanceDate =DateTime.Now;
                    if (referrer != null)
                    {
                        referrerWallet.Balance += referrer.Amount * _dbContext.ReferralTier.FirstOrDefault(r => r.Tier == referrer.Tier)?.Bonus;
                    }
                }
                await _dbContext.SaveAsync();
            }
            return  userWallet;
        }
    }
}
