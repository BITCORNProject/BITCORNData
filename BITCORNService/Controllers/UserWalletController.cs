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
                            if (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null))
                            {
                                var referralPayoutTotal = await ReferralUtils.TotalReward(_dbContext, referrer);
                                var userRegistrationReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount, "BITCORNFarms", "Registrations reward", _dbContext);
                                var miniumBalanceReward = await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal, "BITCORNFarms", "Minium balance reward", _dbContext);
                            
                                if (miniumBalanceReward && userRegistrationReward)
                                {
                                    
                                    referrerStat.TotalReferralRewards += referralPayoutTotal;
                                    userReferral.Bonus = true;
                                    userReferral.ReferrerBonus = true;
                                    await ReferralUtils.UpdateYtdTotal(_dbContext, referrer, referralPayoutTotal);
                                    await ReferralUtils.LogReferralTx(_dbContext, referrerUser.UserId, referralPayoutTotal, "Minimum balance Reward"); 
                                }
                            }
                        }

                        if (referrer != null
                            && userReferral != null
                            && userReferral.MinimumBalanceDate == null
                            && userReferral.WalletDownloadDate != null
                            && userWallet.Balance >= 1000)
                        {
                            var referralPayoutTotal = await ReferralUtils.TotalReward(_dbContext, referrer);
                            var minimumBalanceReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount, "BITCORNFarms", "Minimum balance Reward", _dbContext);
                            var bonusReward = await TxUtils.SendFromBitcornhub(user, 4200, "BITCORNFarms", "Referral bonus reward", _dbContext);
                            userReferral.MinimumBalanceDate = DateTime.Now;

                            if (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null))
                            {
                                if (minimumBalanceReward && bonusReward)
                                {
                                    
                                    userReferral.Bonus = true;
                                    var minBalanceReward= await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal, "BITCORNFarms", "Minimum balance reward", _dbContext);
                                    var referrerBonus = await TxUtils.SendFromBitcornhub(referrerUser, 4200, "BITCORNFarms", "Minimum balance reward", _dbContext);
                                    if (referrerBonus && minBalanceReward)
                                    {
                                        await ReferralUtils.UpdateYtdTotal(_dbContext, referrer, referralPayoutTotal + 4200);
                                        await ReferralUtils.LogReferralTx(_dbContext, referrerUser.UserId, referralPayoutTotal, "Minimum balance Reward");
                                        await ReferralUtils.LogReferralTx(_dbContext, referrerUser.UserId, 4200, "Referral bonus reward");
                                        referrerStat.TotalReferralRewards += referralPayoutTotal + 4200;
                                    }
                                }
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
