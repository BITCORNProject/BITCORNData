﻿using System;
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
                        if (referrer != null && (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null)))
                        {
                            var referralPayoutTotal = await ReferralUtils.TotalReward(dbContext, referrer);
                            var referrerUser= await dbContext.User.FirstOrDefaultAsync( u => u.UserId == referrer.UserId);
                            var referreeReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount, "BITCORNfarms","Social sync", dbContext);
                            var referrerReward = await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal, "BITCORNfarms","Social sync", dbContext);

                            if (referrerReward && referreeReward)
                            {
                                userReferral.SyncDate = DateTime.Now;
                                await UpdateYtdTotal(dbContext, referrer, referralPayoutTotal);
                                await LogReferralTx(dbContext, referrer.UserId, referralPayoutTotal, "Social Sync");
                                var userStat = await dbContext.UserStat.FirstOrDefaultAsync(s => s.UserId == referrer.UserId);
                                userStat.TotalReferralRewardsCorn += referralPayoutTotal;
                                userStat.TotalReferralRewardsUsdt += (referralPayoutTotal * Convert.ToDecimal(await ProbitApi.GetCornPriceAsync()));
                                await ReferralUtils.BonusPayout(dbContext, userReferral, referrer, user, referrerUser, userStat);

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

        public static async Task LogReferralTx(BitcornContext dbContext, int referrerUserId, decimal amount, string type)
        {
            var referralTx = new ReferralTx();
            referralTx.UserId = referrerUserId;
            referralTx.Amount = amount;
            referralTx.TimeStamp = DateTime.Now;
            referralTx.UsdtPrice = await CornPrice(dbContext);
            referralTx.TotalUsdtValue = referralTx.Amount * referralTx.UsdtPrice;
            referralTx.Type = type;
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
            referrer.YtdTotal += (amount * Convert.ToDecimal(await ProbitApi.GetCornPriceAsync()));
            await dbContext.SaveAsync();
        }

        public static async  Task SetTier(BitcornContext dbContext,Referrer referrer)
        {
            var stats = await dbContext.UserStat.FirstOrDefaultAsync(s=>s.UserId == referrer.UserId);

            if (stats.TotalReferrals < 50)
            {
                referrer.Tier = 0;
            }
            if (stats.TotalReferrals >= 50)
            {
                referrer.Tier = 1;
            }
            if (stats.TotalReferrals >= 500)
            {
                referrer.Tier = 2;
            }
            if (stats.TotalReferrals >= 20000)
            {
                referrer.Tier = 3;
            }

            await dbContext.SaveAsync();
        }

        public static async Task<decimal> TotalReward(BitcornContext dbContext, Referrer referrer)
        {
            await SetTier(dbContext, referrer);
            var referralTier = await dbContext.ReferralTier.FirstOrDefaultAsync(r => r.Tier == referrer.Tier);
            return referrer.Amount * referralTier.Bonus;
        }

        public static async Task BonusPayout(BitcornContext dbContext, UserReferral userReferral, Referrer referrer, User user, User referrerUser,
            UserStat referrerStat)
        {
            if (userReferral != null
                && userReferral.SignupReward != null
                && userReferral.MinimumBalanceDate != null
                && userReferral.WalletDownloadDate != null
                && userReferral.SyncDate != null
                && userReferral.Bonus == null
                && userReferral.ReferrerBonus == null
                && referrer != null)
            {
                var bonusReward = await TxUtils.SendFromBitcornhub(user, 4200, "BITCORNFarms", "Referral bonus reward", dbContext);

                if (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null))
                {
                    if (bonusReward)
                    {
                        userReferral.Bonus = DateTime.Now;
                        var referrerBonusReward = await TxUtils.SendFromBitcornhub(referrerUser, 4200, "BITCORNFarms", "Referral bonus reward", dbContext);
                        if (referrerBonusReward)
                        {
                            await UpdateYtdTotal(dbContext, referrer,  4200);
                            await LogReferralTx(dbContext, referrerUser.UserId, 4200, "Referral bonus reward");
                            referrerStat.TotalReferralRewardsCorn +=  4200;
                            referrerStat.TotalReferralRewardsUsdt += (4200 * Convert.ToDecimal(await ProbitApi.GetCornPriceAsync()));
                            userReferral.ReferrerBonus = DateTime.Now;
                        }
                    }
                }
            }
        }


    }
}
