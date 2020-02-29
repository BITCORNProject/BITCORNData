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
            if (!UserLockCollection.Lock(userReferral.UserId))
            {
                return StatusCode(UserLockCollection.UserLockedReturnCode);
            }

            try
            {
                
                if (walletDownload.ReferralUserId != 0)
                {
                    try
                    {
                        walletDownload.ReferralUserId = _dbContext.Referrer
                            .FirstOrDefault(r => r.ReferralId == Convert.ToInt32(walletDownload.ReferralCode))?.UserId;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"could not convert referral code: {walletDownload.ReferralCode} to an integer");
                    }
                    var referrerUser = await _dbContext.JoinUserModels().FirstOrDefaultAsync(u=>u.UserId==walletDownload.ReferralUserId);
                   
                    if (referrerUser != null && userReferral != null && userReferral?.WalletDownloadDate == null)

                    {
                        var referrerWallet = referrerUser.UserWallet;

                        var referrer = await _dbContext.Referrer
                            .FirstOrDefaultAsync(w => w.UserId == walletDownload.ReferralUserId);
                        
                        if (referrer != null)
                        {
                            var referrerReward = await TxUtils.SendFromBitcornhub(referrerUser, referrer.Amount, "BITCORNFarms", "Referral", _dbContext);
                            //todo log referral tx
                            if (referrerReward)
                            {
                                referrerUser.UserStat.TotalReferralRewards += referrer.Amount + 10;
                            }
                        }

                        if (userReferral.MinimumBalanceDate != null
                            && userReferral.WalletDownloadDate == null)
                        {
                            var userWallet = _dbContext.JoinUserModels().FirstOrDefault(w => w.UserId == walletDownload.UserId);
                            if (userWallet != null)
                            {
                                var downloadReward = await TxUtils.SendFromBitcornhub(userWallet, 100, "BITCORNFarms", "Wallet download", _dbContext);
                                await ReferralUtils.LogReferralTx(_dbContext, (int) walletDownload.ReferralUserId, 100, walletDownload.ReferralId );
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
                                //todo log referral tx
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
                UserLockCollection.Release(userReferral.UserId);
            }
        }

    }
}
