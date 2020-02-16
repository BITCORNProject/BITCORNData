using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
                    if (referrerWallet != null)
                    {
                        var amount = _dbContext.Referrer
                            .FirstOrDefault(w => w.UserId == walletDownload.ReferralUserId)?.amount;
                        referrerWallet.Balance += amount;
                        var botWallet = _dbContext.UserWallet.FirstOrDefault(w => w.UserId == 196);
                        if (botWallet != null)
                        {
                            botWallet.Balance -= amount;
                            await _dbContext.SaveAsync();
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
                await BITCORNLogger.LogError(_dbContext, e);
                return HttpStatusCode.InternalServerError;
            }
        }

    }
}
