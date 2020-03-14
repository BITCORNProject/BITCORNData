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
                        await ReferralUtils.ReferralRewards(_dbContext, walletDownload, userReferral, referrerUser, user, "Wallet download");
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
