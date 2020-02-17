using System;
<<<<<<< HEAD
=======
using System.Collections.Generic;
>>>>>>> 9016a62390c1843050fb4078be2075b1ce7eeecb
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
<<<<<<< HEAD
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
=======
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
>>>>>>> 9016a62390c1843050fb4078be2075b1ce7eeecb

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
        public async Task<HttpStatusCode> Post([FromBody] WalletDownload walletDownload)
        {
            try
            {
<<<<<<< HEAD
                var previousDownload =
                    _dbContext.WalletDownload.Where(d => d.IPAddress == walletDownload.IPAddress);
=======
>>>>>>> 9016a62390c1843050fb4078be2075b1ce7eeecb
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
                    var referrerWallet = _dbContext.UserWallet.FirstOrDefault(w => w.UserId == walletDownload.ReferralUserId);
<<<<<<< HEAD
                    if (referrerWallet != null  && !previousDownload.Any())
=======
                    if (referrerWallet != null)
>>>>>>> 9016a62390c1843050fb4078be2075b1ce7eeecb
                    {
                        var amount = _dbContext.Referrer
                            .FirstOrDefault(w => w.UserId == walletDownload.ReferralUserId)?.Amount;
                        referrerWallet.Balance += amount;
                        var botWallet = _dbContext.UserWallet.FirstOrDefault(w => w.UserId == 196);
                        if (botWallet != null)
                        {
                            botWallet.Balance -= amount;
                            await _dbContext.SaveAsync();
<<<<<<< HEAD
                            var stats = _dbContext.UserStat.FirstOrDefault(s => s.UserId == walletDownload.ReferralUserId);
                            stats.TotalReferrals += 1;
                            stats.TotalReferralRewards += amount;
=======
                            _dbContext.UserStat.FirstOrDefault(s => s.UserId == walletDownload.ReferralUserId);
>>>>>>> 9016a62390c1843050fb4078be2075b1ce7eeecb
                        }
                    }
                }

                walletDownload.TimeStamp = DateTime.Now;
                _dbContext.WalletDownload.Add(walletDownload);
                await _dbContext.SaveChangesAsync();
                return HttpStatusCode.OK;
            }
            catch (Exception e)
            {
<<<<<<< HEAD
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(walletDownload));
=======
                await BITCORNLogger.LogError(_dbContext, e);
>>>>>>> 9016a62390c1843050fb4078be2075b1ce7eeecb
                return HttpStatusCode.InternalServerError;
            }
        }

    }
}
