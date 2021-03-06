﻿using System;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Authorize(Policy = AuthScopes.ReadUser)]
    [Route("api/[controller]")]
    [ApiController]
    public class UserWalletController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        public const decimal MIN_BALANCE_QUEST_AMOUNT = 1000;
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

                        await ReferralUtils.BonusPayout(_dbContext, userReferral, referrer, user, referrerUser, referrerStat);

                        if (referrer != null &&
                            userReferral != null &&
                            userReferral.MinimumBalanceDate == null &&
                            userWallet.Balance >= MIN_BALANCE_QUEST_AMOUNT)
                        {
                            if (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null))
                            {
                                var referralPayoutTotal = await ReferralUtils.TotalReward(_dbContext, referrer);
                                var miniumBalanceRewardUser = await TxUtils.SendFromBitcornhub(user, referrer.Amount, "BITCORNFarms", "Minium balance reward", _dbContext);
                                var miniumBalanceReward = await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal, "BITCORNFarms", "Minium balance reward", _dbContext);

                                if (miniumBalanceReward && miniumBalanceRewardUser)
                                {

                                    referrerStat.TotalReferralRewardsCorn += referralPayoutTotal;
                                    referrerStat.TotalReferralRewardsUsdt += (referralPayoutTotal * (await ProbitApi.GetCornPriceAsync(_dbContext)));
                                    userReferral.MinimumBalanceDate = DateTime.Now;
                                    await ReferralUtils.UpdateYtdTotal(_dbContext, referrer, referralPayoutTotal);
                                    await ReferralUtils.LogReferralTx(_dbContext, user.UserId, referrer.Amount, "Recruit minimum balance Reward");
                                    await ReferralUtils.LogReferralTx(_dbContext, referrerUser.UserId, referralPayoutTotal, "Minimum balance Reward");
                                    await ReferralUtils.BonusPayout(_dbContext, userReferral, referrer, user, referrerUser, referrerStat);
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
