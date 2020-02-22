using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.Models;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils
{
    public static class ReferralUtils
    {
        public static void UpdateReferralSync(BitcornContext dbContext, PlatformId platformId)
        {
            int? userId =0;
            switch (platformId.Platform)
            {
                case "twitch":
                    userId = dbContext.UserIdentity.FirstOrDefault(u => platformId != null && u.TwitchId == platformId.Id)?.UserId;
                    break;
                case "twitter":
                    userId = dbContext.UserIdentity.FirstOrDefault(u => platformId != null && u.TwitterId == platformId.Id)?.UserId;
                    break;
                case "discord":
                    userId = dbContext.UserIdentity.FirstOrDefault(u => platformId != null && u.DiscordId == platformId.Id)?.UserId;
                    break;
                case "reddit":
                    userId = dbContext.UserIdentity.FirstOrDefault(u => platformId != null && u.RedditId == platformId.Id)?.UserId;
                    break;
            }

            if (userId != 0 && userId != null)
            {
                var userReferral = dbContext.UserReferral.FirstOrDefault(u => u.UserId == userId);
                if (userReferral != null && userReferral.SyncDate == null)
                {
                    var referrer =
                        dbContext.Referrer.FirstOrDefault(r => r.ReferralId == userReferral.ReferralId);
                    var referrerWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == referrer.UserId);
                    if (referrer != null)
                        if (referrerWallet != null)
                            referrerWallet.Balance += referrer.Amount;

                    var botWallet = dbContext.UserWallet.FirstOrDefault(w => w.UserId == 196);
                    if (botWallet != null)
                    {
                        if (referrer != null)
                        {
                            botWallet.Balance -= referrer.Amount;
                            var stats = dbContext.UserStat.FirstOrDefault(s => s.UserId == referrer.UserId);
                            if (stats != null)
                            {
                                stats.TotalReferralRewards += referrer.Amount;
                            }
                        }
                    }
                    userReferral.SyncDate = DateTime.Now;
                }
            }
        }
    }
}
