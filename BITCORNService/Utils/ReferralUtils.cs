using System;
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
                    if (user.IsBanned)
                        return;
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
                            var referrerUser = await dbContext.User.FirstOrDefaultAsync(u => u.UserId == referrer.UserId);
                            if (referrerUser.IsBanned)
                            {
                                if (user != null)
                                    UserLockCollection.Release(user.UserId);
                                return;
                            }
                            var referreeReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount, "BITCORNfarms", "Recruit social sync", dbContext);
                            var referrerReward = await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal, "BITCORNfarms", "Social sync", dbContext);

                            if (referrerReward && referreeReward)
                            {
                                userReferral.SyncDate = DateTime.Now;
                                await UpdateYtdTotal(dbContext, referrer, referralPayoutTotal);
                                await LogReferralTx(dbContext, referrer.UserId, referralPayoutTotal, "Social Sync");
                                await LogReferralTx(dbContext, user.UserId, referralPayoutTotal, "Recruit social Sync");
                                var userStat = await dbContext.UserStat.FirstOrDefaultAsync(s => s.UserId == referrer.UserId);
                                userStat.TotalReferralRewardsCorn += referralPayoutTotal;
                                userStat.TotalReferralRewardsUsdt += (referralPayoutTotal * (await ProbitApi.GetCornPriceAsync(dbContext)));
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
                if (user != null)
                    UserLockCollection.Release(user.UserId);
            }
        }

        public static async Task<ReferralTx> LogReferralTx(BitcornContext dbContext, int referrerUserId, decimal amount, string type)
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
            return referralTx;
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
                    price.LatestPrice = (await ProbitApi.GetCornPriceAsync(dbContext));
                    dbContext.Price.Add(price);
                    await dbContext.SaveAsync();
                    return price.LatestPrice;
                }

                cornPrice.LatestPrice = (await ProbitApi.GetCornPriceAsync(dbContext));
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
            referrer.YtdTotal += (amount * (await ProbitApi.GetCornPriceAsync(dbContext)));
            await dbContext.SaveAsync();
        }

        public static async  Task SetTier(BitcornContext dbContext,Referrer referrer)
        {
            var banned = await dbContext.User.Where(s => s.UserId == referrer.UserId).Select(u=>u.IsBanned).FirstOrDefaultAsync();
            if (banned) return;
            var stats = await dbContext.UserStat.FirstOrDefaultAsync(s=>s.UserId == referrer.UserId);
            if (stats.TotalReferrals == null)
            {
                stats.TotalReferrals = 0;
            }

            if (stats.TotalReferrals < 50 && referrer.Tier < 1)
            {
                referrer.Tier = 0;
            }
            if (stats.TotalReferrals >= 50 && referrer.Tier < 1)
            {
                referrer.Tier = 1;
            }
            if (stats.TotalReferrals >= 500 && referrer.Tier < 2)
            {
                referrer.Tier = 2;
            }
            if (stats.TotalReferrals >= 20000 && referrer.Tier < 3)
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

        public static async Task<decimal> WalletBonusReward(BitcornContext dbContext, Referrer referrer, int amount)
        {
            await SetTier(dbContext, referrer);
            var referralTier = await dbContext.ReferralTier.FirstOrDefaultAsync(r => r.Tier == referrer.Tier);
            return amount * referralTier.Bonus;
        }
        public const decimal BONUS_PAYOUT = 4200;
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
                && userReferral.UserSubscriptionId == null
                && referrer != null
                && !user.IsBanned
                && !referrerUser.IsBanned)
            {
                
                var subQuery = SubscriptionUtils.GetActiveSubscription(dbContext, user, "BITCORNFarms", 1);
                if (subQuery == null)
                    return;
                
                var userSubInfo = await subQuery.FirstOrDefaultAsync();
                if (userSubInfo == null)
                    return;
                
                userReferral.UserSubscriptionId = userSubInfo.UserSubcriptionTierInfo.UserSubscription.UserSubscriptionId;

                var amount = BONUS_PAYOUT;
                var bonusReward = await TxUtils.SendFromBitcornhub(user, amount, "BITCORNFarms", "Referral bonus reward", dbContext);

                if (IsValidReferrer(referrer))
                {
                    if (bonusReward)
                    {
                        userReferral.Bonus = DateTime.Now;
                        await LogReferralTx(dbContext, user.UserId, amount, "Recruit bonus reward");
                        var referrerBonusReward = await TxUtils.SendFromBitcornhub(referrerUser, amount, "BITCORNFarms", "Referral bonus reward", dbContext);
                        if (referrerBonusReward)
                        {
                            await UpdateYtdTotal(dbContext, referrer,  amount);
                            await LogReferralTx(dbContext, referrerUser.UserId, amount, "Referral bonus reward");
                            referrerStat.TotalReferralRewardsCorn +=  amount;
                            referrerStat.TotalReferralRewardsUsdt += (amount * await ProbitApi.GetCornPriceAsync(dbContext));
                            userReferral.ReferrerBonus = DateTime.Now;
                        }
                    }
                }
            }
        }
        
        public static bool IsValidReferrer(Referrer referrer)
        {
            return referrer != null && (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null));
        }

        public static async Task ReferralRewards(BitcornContext dbContext, WalletDownload walletDownload, UserReferral userReferral, User referrerUser,
    User user, string type)
        {
            if (user.IsBanned || referrerUser.IsBanned) return;

            var referrer = await dbContext.Referrer
                .FirstOrDefaultAsync(w => w.UserId == walletDownload.ReferralUserId);

            userReferral.WalletDownloadDate = DateTime.Now;

            if (IsValidReferrer(referrer))
            {
                var referralPayoutTotal = await ReferralUtils.TotalReward(dbContext, referrer) + await ReferralUtils.WalletBonusReward(dbContext, referrer, 10); ;
                var referrerReward = await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal , "BITCORNFarms",
                    type, dbContext);
                var referreeReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount + 10, "BITCORNFarms",
                    type, dbContext);

                if (referrerReward && referreeReward)
                {
                    await ReferralUtils.LogReferralTx(dbContext, referrerUser.UserId, referralPayoutTotal ,
                        type);
                    await ReferralUtils.LogReferralTx(dbContext, user.UserId, referrer.Amount + 10,
                        $"Recruit {type}");
                    await ReferralUtils.UpdateYtdTotal(dbContext, referrer, referralPayoutTotal );
                    if (referrerUser.UserStat.TotalReferralRewardsCorn == null)
                    {
                        referrerUser.UserStat.TotalReferralRewardsCorn = 0;
                    }
                    if (referrerUser.UserStat.TotalReferralRewardsUsdt == null)
                    {
                        referrerUser.UserStat.TotalReferralRewardsUsdt = 0;
                    }
                    referrerUser.UserStat.TotalReferralRewardsCorn += referralPayoutTotal ;
                    referrerUser.UserStat.TotalReferralRewardsUsdt +=
                        ((referralPayoutTotal ) * (await ProbitApi.GetCornPriceAsync(dbContext)));
                    userReferral.WalletDownloadDate = DateTime.Now;
                    await ReferralUtils.BonusPayout(dbContext, userReferral, referrer, user, referrerUser,
                        referrerUser.UserStat);
                }
            }
        }


    }
}
