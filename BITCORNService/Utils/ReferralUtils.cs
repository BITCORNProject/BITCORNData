using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils
{
    public static class ReferralUtils
    {
        public static async Task UpdateReferralSync(BitcornContext dbContext, PlatformId platformId)
        {
            try
            {
                int? userId = BitcornUtils.GetUserForPlatform(platformId, dbContext).Select(u => u.UserId).FirstOrDefault();

                if (userId != 0 && userId != null)
                {
                    var userReferral = await dbContext.UserReferral.FirstOrDefaultAsync(u => u.UserId == userId);
                    if (userReferral != null && userReferral.SyncDate == null)
                    {
                        var referrer = await 
                            dbContext.Referrer.FirstOrDefaultAsync(r => r.ReferralId == userReferral.ReferralId);
                        var referrerWallet = await dbContext.UserWallet.FirstOrDefaultAsync(w => w.UserId == referrer.UserId);
                        if (referrer != null)
                            if (referrerWallet != null)
                                referrerWallet.Balance += referrer.Amount;

                        var botWallet = await dbContext.UserWallet.FirstOrDefaultAsync(w => w.UserId == 196);
                        if (botWallet != null)
                        {
                            if (referrer != null)
                            {
                                botWallet.Balance -= referrer.Amount;
                                var stats = await dbContext.UserStat.FirstOrDefaultAsync(s => s.UserId == referrer.UserId);
                                if (stats != null)
                                {
                                    stats.TotalReferralRewards += referrer.Amount;
                                }
                            }
                        }
                        userReferral.SyncDate = DateTime.Now;
                    }

                    await dbContext.SaveAsync();
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(dbContext, e, null);
                throw;
            }
        }
    }
}
