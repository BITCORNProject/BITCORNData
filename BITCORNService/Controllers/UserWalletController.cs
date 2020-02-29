using System;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Tx;
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
            var user= await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            
            if (user != null)
            {
                var userWallet = user.UserWallet;
                if (!UserLockCollection.Lock(userWallet.UserId))
                {
                    return userWallet;
                }
                try
                {
                    var referralId = _dbContext.UserReferral.FirstOrDefault(r => r.UserId == userWallet.UserId)?.ReferralId;
                    if (referralId != 0 && referralId != null)
                    {
                        var userReferral = await _dbContext.UserReferral.FirstOrDefaultAsync(r => r.UserId == userWallet.UserId);
                        var referrer = await _dbContext.Referrer.FirstOrDefaultAsync(r => r.ReferralId == userReferral.ReferralId);
                        var referrerUser = await _dbContext.JoinUserModels().FirstOrDefaultAsync(w => w.UserId == referrer.UserId);
                        var referrerStat = await _dbContext.UserStat.FirstOrDefaultAsync(s => s.UserId == referrer.UserId);

                        if (userReferral != null
                           && userReferral.SignupReward
                           && userReferral.MinimumBalanceDate != null
                           && userReferral.WalletDownloadDate != null
                           && userReferral.Bonus == false
                           && userReferral.ReferrerBonus == false
                           && referrer != null)
                        {
                            var miniumBalanceReward = await TxUtils.SendFromBitcornhub(referrerUser, referrer.Amount, "BITCORNFarms", "Minium balance reward", _dbContext);
                            //todo log referral tx
                            if (miniumBalanceReward)
                            {
                                referrerStat.TotalReferralRewards += referrer.Amount;
                                userReferral.Bonus = true;
                                userReferral.ReferrerBonus = true;
                            }
                        }

                        if (referrer != null
                            && userReferral != null
                            && userReferral.MinimumBalanceDate == null
                            && userReferral.WalletDownloadDate != null
                            && userWallet.Balance >= 1000)
                        {
                            var bonus = _dbContext.ReferralTier.FirstOrDefault(r => r.Tier == referrer.Tier).Bonus;


                            var minimumBalanceReward = await TxUtils.SendFromBitcornhub(user, 100, "BITCORNFarms", "Referral bonus reward", _dbContext);
                            //todo log referral tx
                            if (minimumBalanceReward)
                            {
                                userReferral.MinimumBalanceDate = DateTime.Now;
                                userReferral.Bonus = true;
                                
                                await TxUtils.SendFromBitcornhub(referrerUser, referrer.Amount * bonus, "BITCORNFarms", "Minimum balance reward", _dbContext);
                                //todo log referral tx
                                referrerStat.TotalReferralRewards += referrer.Amount * bonus;
                            }
                        }
                        await _dbContext.SaveAsync();
                    }
                }
                catch (Exception e)
                {
                    await BITCORNLogger.LogError(_dbContext, e, id);
                }
                finally
                {
                    UserLockCollection.Release(userWallet.UserId);
                }
                return user.UserWallet;
            }
            else
            {
                return null;
            }
        }
    }
}
