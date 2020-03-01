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
            var userReferral = _dbContext.UserReferral.FirstOrDefault(r => r.UserId == walletDownload.UserId);
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
                        
                        if (referrer != null)
                        {
                            var referrerReward = await TxUtils.SendFromBitcornhub(referrerUser, referrer.Amount, "BITCORNFarms", "Referral wallet download", _dbContext);
                            await ReferralUtils.LogReferralTx(_dbContext, referrerUser.UserId, referrer.Amount, "Wallet download");
                            await ReferralUtils.UpdateYtdTotal(_dbContext, referrer, referrer.Amount);
                            if (referrerReward)
                            {
                                referrerUser.UserStat.TotalReferralRewards += referrer.Amount + 10;
                                userReferral.WalletDownloadDate = DateTime.Now;
                            }
                        }

                        if (userReferral.MinimumBalanceDate != null
                            && userReferral.WalletDownloadDate == null)
                        {
                            var userWallet = _dbContext.JoinUserModels().FirstOrDefault(w => w.UserId == walletDownload.UserId);
                            if (userWallet != null)
                            {
                                var downloadReward = await TxUtils.SendFromBitcornhub(userWallet, 100, "BITCORNFarms", "Wallet download", _dbContext);
                                if (downloadReward)
                                {
                                    userReferral.Bonus = true;
                                }
                            }
                            if (referrer != null)
                            {
                                var referralTier = _dbContext.ReferralTier.FirstOrDefault(r => r.Tier == referrer.Tier);
                                var amount = referrer.Amount;
                                if (referralTier != null) amount *= referralTier.Bonus;

                                var referrerReward = await TxUtils.SendFromBitcornhub(referrerUser, amount, "BITCORNFarms", "Referral", _dbContext);
                                await ReferralUtils.UpdateYtdTotal(_dbContext, referrer, referrer.Amount);
                                await ReferralUtils.LogReferralTx(_dbContext, referrer.UserId, amount , "Wallet download");
                                if (referrerReward)
                                {
                                    userReferral.WalletDownloadDate = DateTime.Now;
                                }

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
