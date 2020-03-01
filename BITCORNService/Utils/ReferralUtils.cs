using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Utils
{
    public static class ReferralUtils
    {
        static decimal _CornPrice;

        public static async Task UpdateReferralSync(BitcornContext dbContext, PlatformId platformId)
        {
            var user = BitcornUtils.GetUserForPlatform(platformId, dbContext).FirstOrDefault();
            try
            {
                if (user != null && user.UserId != 0)
                {
                    if (!UserLockCollection.Lock(user.UserId))
                    {
                        throw new Exception("User is locked");
                    }

                    var userReferral = await dbContext.UserReferral.FirstOrDefaultAsync(u => u.UserId == user.UserId);
                    if (userReferral != null && userReferral.SyncDate == null)
                    {
                        var referrer = await
                            dbContext.Referrer.FirstOrDefaultAsync(r => r.ReferralId == userReferral.ReferralId);
                        if (referrer != null)
                        {
                            var referrerReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount, "BITCORNfarms",
                                "Referral", dbContext);
                            await UpdateYtdTotal(dbContext, referrer, referrer.Amount);
                            await LogReferralTx(dbContext, user.UserId, referrer.Amount);
                            if (referrerReward)
                            {
                                userReferral.SyncDate = DateTime.Now;
                                var userStats =
                                    await dbContext.UserStat.FirstOrDefaultAsync(s => s.UserId == userReferral.UserId);
                                userStats.TotalReferrals += 1;
                            }
                        }
                    }

                    await dbContext.SaveAsync();
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(dbContext, e, null);
                throw;
            }
            finally
            {
                UserLockCollection.Release(user.UserId);
            }
        }

        public static async Task LogReferralTx(BitcornContext dbContext, int referrerUserId, decimal amount)
        {
            var referralTx = new ReferralTx();
            referralTx.UserId = referrerUserId;
            referralTx.Amount = amount;
            referralTx.TimeStamp = DateTime.Now;
            referralTx.UsdtPrice = await CornPrice(dbContext);
            referralTx.TotalUsdtValue = referralTx.Amount * referralTx.UsdtPrice;
            dbContext.ReferralTx.Add(referralTx);
            await dbContext.SaveAsync();
        }

        public static async Task<decimal> CornPrice(BitcornContext dbContext)
        {

            var cornPrice = dbContext.Price.FirstOrDefault(p => p.Symbol == "CORN");
            try
            {
                if (cornPrice == null)
                {
                    var price = new Price();
                    price.Symbol = "CORN";
                    price.LatestPrice = Convert.ToDecimal(await ProbitApi.GetCornPriceAsync());
                    dbContext.Price.Add(price);
                    await dbContext.SaveAsync();
                    return price.LatestPrice;
                }

                cornPrice.LatestPrice = Convert.ToDecimal(await ProbitApi.GetCornPriceAsync());
                await dbContext.SaveAsync();
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(dbContext,e,null);
            }
            return cornPrice.LatestPrice;
        }

        public static async Task UpdateYtdTotal(BitcornContext dbContext, Referrer referrer, decimal amount)
        {
            referrer.YtdTotal += amount;
            await dbContext.SaveAsync();
        }
    }
}
