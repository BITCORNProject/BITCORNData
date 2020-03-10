using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;


namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WalletDownloadController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public WalletDownloadController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        // POST: api/WalletDownload
        [HttpPost]

        public async Task<ActionResult> Post([FromBody] WalletDownload walletDownload)
        {
            var user = await _dbContext.User.FirstOrDefaultAsync(r => r.UserId == walletDownload.UserId);
            var userReferral = await _dbContext.UserReferral.FirstOrDefaultAsync(r => r.UserId == walletDownload.UserId);
            if (userReferral != null && !UserLockCollection.Lock(userReferral.UserId))
            {
                return StatusCode(UserLockCollection.UserLockedReturnCode);
            }

            try
            {
                if (userReferral != null && (userReferral.ReferralId != 0 && userReferral.WalletDownloadDate == null)) 
                {
                    try
                    {
                        walletDownload.ReferralUserId = _dbContext.Referrer
                            .FirstOrDefault(r => r.ReferralId == Convert.ToInt32(userReferral.ReferralId))?.UserId;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"could not convert referral code: {walletDownload.ReferralCode} to an integer");
                    }

                    var referrerUser = await _dbContext.JoinUserModels().FirstOrDefaultAsync(u=>u.UserId==walletDownload.ReferralUserId);

                    if (referrerUser != null && userReferral != null && userReferral?.WalletDownloadDate == null)
                    {
                        var referrer = await _dbContext.Referrer
                            .FirstOrDefaultAsync(w => w.UserId == walletDownload.ReferralUserId);

                        userReferral.WalletDownloadDate = DateTime.Now;

                        if (referrer != null && (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null)))
                        {
                            var referralPayoutTotal = await ReferralUtils.TotalReward( _dbContext, referrer);
                            var referrerReward = await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal + 10, "BITCORNFarms", "Referral wallet download", _dbContext);
                            var referreeReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount + 10, "BITCORNFarms", "Referral wallet download", _dbContext);

                            if (referrerReward && referreeReward)
                            {
                                await ReferralUtils.LogReferralTx(_dbContext, referrerUser.UserId, referralPayoutTotal + 10, "Wallet download");
                                await ReferralUtils.UpdateYtdTotal(_dbContext, referrer, referralPayoutTotal + 10);
                                referrerUser.UserStat.TotalReferralRewardsCorn += referralPayoutTotal + 10;
                                referrerUser.UserStat.TotalReferralRewardsUsdt += ((referralPayoutTotal + 10) * Convert.ToDecimal(await ProbitApi.GetCornPriceAsync()));
                                userReferral.WalletDownloadDate = DateTime.Now;
                                await ReferralUtils.BonusPayout(_dbContext, userReferral, referrer, user, referrerUser, referrerUser.UserStat);
                            }
                        }
                    }
                }

                walletDownload.TimeStamp = DateTime.Now;
                _dbContext.WalletDownload.Add(walletDownload);
                await _dbContext.SaveAsync();
                return StatusCode((int)HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(walletDownload));
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
            finally
            {
                if (userReferral != null) UserLockCollection.Release(userReferral.UserId);
            }
        }

    }
}
