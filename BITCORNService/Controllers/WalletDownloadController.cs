using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<HttpStatusCode> Post([FromBody] WalletDownload walletDownload)
        {
            try
            {
                var userReferral = _dbContext.UserReferral.FirstOrDefault(r => r.UserId == walletDownload.UserId);

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

                    if (referrerWallet != null && userReferral != null && userReferral?.WalletDownloadDate == null)

                    {
                        decimal amount = _dbContext.Referrer
                            .FirstOrDefault(w => w.UserId == walletDownload.ReferralUserId).Amount;
                        referrerWallet.Balance += amount;
                        var botWallet = _dbContext.UserWallet.FirstOrDefault(w => w.UserId == 196);
                        if (botWallet != null)
                        {
                            botWallet.Balance -= amount;
                            var stats = _dbContext.UserStat.FirstOrDefault(s => s.UserId == walletDownload.ReferralUserId);
                            if (stats != null)
                            {
                                stats.TotalReferralRewards += amount + 10;
                            }
                        }

                        if (userReferral.MinimumBalanceDate != null
                            && userReferral.WalletDownloadDate == null)
                        {
                            var referrer = _dbContext.Referrer.FirstOrDefault(r => r.ReferralId == walletDownload.ReferralUserId);
                            var userWallet = _dbContext.UserWallet.FirstOrDefault(w => w.UserId == walletDownload.UserId);
                            if (userWallet != null)
                            {
                                userWallet.Balance += 100;
                            }
                            if (referrer != null)
                            { 
                                referrerWallet.Balance += referrer.Amount *
                                                          _dbContext.ReferralTier
                                                              .FirstOrDefault(r => r.Tier == referrer.Tier)?
                                                              .Bonus;

                            }
                        }

                        userReferral.WalletDownloadDate = DateTime.Now;
                    }
                }

                walletDownload.TimeStamp = DateTime.Now;
                _dbContext.WalletDownload.Add(walletDownload);
                await _dbContext.SaveAsync();
                return HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(walletDownload));
                return HttpStatusCode.InternalServerError;
            }
        }

    }
}
