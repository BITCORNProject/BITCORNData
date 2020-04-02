using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BITCORNService.Utils
{
    public static class SubscriptionUtils
    {
        public static bool HasExpired(this Subscription subscription, UserSubscription user)
        {
            return DateTime.Now > user.LastSubDate.Value.AddDays(subscription.Duration);
        }

        public static void CreateSubscription(BitcornContext dbContext, Subscription sub, params SubscriptionTier[] tiers)
        {
            dbContext.Subscription.Add(sub);
            dbContext.SaveChanges();

            foreach (var tier in tiers)
            {
                tier.SubscriptionId = sub.SubscriptionId;
                dbContext.SubscriptionTier.Add(tier);
            }

            dbContext.SaveChanges();
        }

        public static IQueryable<UserSubcriptionTierInfo> GetUserSubscriptions(BitcornContext dbContext, User user)
        {
            return (from userSubscription in dbContext.UserSubscription
                                           join subscriptionTier in dbContext.SubscriptionTier
                                           on userSubscription.SubscriptionTierId equals subscriptionTier.SubscriptionTierId
                                           select new UserSubcriptionTierInfo
                                           {
                                               UserSubscription = userSubscription,
                                               SubscriptionTier = subscriptionTier
                                           }).Where(t => t.UserSubscription.UserId == user.UserId);

        }

        public static decimal CalculateUsdtToCornCost(decimal cornUsdt,SubscriptionTier tier)
        {
            return Math.Ceiling(tier.CostUsdt.Value / cornUsdt);
        }

        public static async Task<decimal> CalculateUsdtToCornCost(SubscriptionTier tier)
        {
            var price = await ProbitApi.GetCornPriceAsync();
            return CalculateUsdtToCornCost(price,tier);
        }
        public static async Task<SubscriptionResponse> Subscribe(BitcornContext dbContext, User user, SubRequest subRequest)
        {
            try
            {
                //try to find subscription
                var subInfo = await dbContext.Subscription.FirstOrDefaultAsync(s => s.Name.ToLower() == subRequest.SubscriptionName.ToLower());
                //initialize response object
                var output = new SubscriptionResponse();
                if (subInfo != null)
                {
                    output.RequestedSubscriptionInfo = subInfo;
                    //try to find subscription tier info
                    var requestedTierInfo = await dbContext.SubscriptionTier.FirstOrDefaultAsync(t => t.SubscriptionId == subInfo.SubscriptionId && t.Tier == subRequest.Tier);
                    
                    output.RequestedSubscriptionTier = requestedTierInfo;
                    //if tier was found, attempt to process the subscription
                    if (requestedTierInfo != null)
                    {
                        return await ProcessSubscription(dbContext, output, subRequest, subInfo, requestedTierInfo, user);
                    }
                    else
                    {
                        //this subscription cannot be executed, fill out the response object
                        await PopuplateUserResponse(dbContext, subRequest, output, user);
                    }
                }
                else
                {
                    //this subscription cannot be executed, fill out the response object
                    await PopuplateUserResponse(dbContext, subRequest, output, user);
                }

                return output;
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(dbContext,e,null);
                throw e;
            }
        }

        private static async Task<SubscriptionResponse> ProcessSubscription(BitcornContext dbContext, SubscriptionResponse output, SubRequest subRequest, Subscription subInfo, SubscriptionTier requestedTierInfo,User user)
        {
            decimal cost = 0;
            //if tier usdt cost has been initialized, the corn cost has to be calculated
            if (requestedTierInfo.CostUsdt != null && requestedTierInfo.CostUsdt > 0)
            {
                cost = await CalculateUsdtToCornCost(requestedTierInfo);
            }
            // check if cost is initialized properly
            else if (requestedTierInfo.CostCorn != null && requestedTierInfo.CostCorn > 0)
            {
                cost = requestedTierInfo.CostCorn.Value;
            }
            else
            {
                throw new ArgumentException($"Invalid cost setting on subscription tier id:{requestedTierInfo.SubscriptionId}");
            }
            //set the amount that will be removed from subscriber to the response object
            output.Cost = cost;
            //initialize array of existing subscriptions
            UserSubcriptionTierInfo[] existingSubscriptions = new UserSubcriptionTierInfo[0];
            if (user != null)
            {
                //set data to existing subscriptions array
                existingSubscriptions = await GetUserSubscriptions(dbContext, user)
                    .Where(t => t.SubscriptionTier.SubscriptionId == subInfo.SubscriptionId).ToArrayAsync();
            }
            //initialize reference to existing subtierinfo
            UserSubcriptionTierInfo existingSubscription = null;
            //initialize current substate 
            var subState = SubscriptionState.None;
            //if any subscriptions were found
            if (existingSubscriptions.Any())
            {
                //set existing subtierinfo
                existingSubscription = existingSubscriptions[0];
                //if sub has expired, set substate to expired
                if (subInfo.HasExpired(existingSubscription.UserSubscription))
                    subState = SubscriptionState.Expired;
                //if existing sub has not expired, but the tier is below, set subState to TierDown
                else if (existingSubscription.SubscriptionTier.Tier < requestedTierInfo.Tier)
                    subState = SubscriptionState.TierDown;
                //else, the user is subscribed 
                else subState = SubscriptionState.Subscribed;
            }
            //initialize reference to usersubscription & tx request
            UserSubscription sub = null;
            TxRequest txRequest = null;
            //if current user sub state is not subscribed & the client confirmed the cost to be equal to the cost amount, attempt to subscribe
            if (subState != SubscriptionState.Subscribed && subRequest.Amount == cost)
            {
                //initialize recipient of the transaction
                string[] to = new string[1];
                //default to bitcornhub if no subscription owner has been set
                int recipientId = TxUtils.BitcornHubPK;
                //if subscription owner is set, overwrite bitcornhub
                if (subInfo.OwnerUserId != null && subInfo.OwnerUserId>0)
                    recipientId = subInfo.OwnerUserId.Value;
              
                to[0] = $"userid|{recipientId}";
                //initialize tx request
                txRequest = new TxRequest(user, cost, subRequest.Platform, "$sub", to);
                //prepare transaction for saving
                var processInfo = await TxUtils.ProcessRequest(txRequest, dbContext);
                var transactions = processInfo.Transactions;
                if (transactions != null && transactions.Length > 0)
                {
                    StringBuilder sql = new StringBuilder();
                    //check if transaction can be executed
                    if (processInfo.WriteTransactionOutput(sql))
                    {
                        //transaction is ready to be saved
                        switch (subState)
                        {
                            case SubscriptionState.None:
                                //user was previously not subscribed, create instance of usersubscription and point it to the user
                                sub = new UserSubscription();
                                sub.SubscriptionId = subInfo.SubscriptionId;
                                sub.SubscriptionTierId = requestedTierInfo.SubscriptionTierId;
                                sub.UserId = user.UserId;
                                sub.FirstSubDate = DateTime.Now;
                                dbContext.UserSubscription.Add(sub);
                                break;
                            case SubscriptionState.TierDown:
                            case SubscriptionState.Expired:
                                //previous subscription was found, update subscription tier
                                existingSubscription.UserSubscription.SubscriptionTierId = requestedTierInfo.SubscriptionTierId;
                                existingSubscription.UserSubscription.SubCount += 1;
                                sub = existingSubscription.UserSubscription;
                                break;
                            default:
                                break;
                        }
                        //set subscription date to now
                        sub.LastSubDate = DateTime.Now;

                        await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                        await dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                        //create subtx that will link user, corntx and usersubscription together
                        var subTx = new SubTx();
                        subTx.UserId = user.UserId;
                        subTx.SubTxId = transactions[0].TxId.Value;
                        subTx.UserSubscriptionId = sub.UserSubscriptionId;
                        dbContext.SubTx.Add(subTx);

                        //if user was not subscribed before, attempt to share the payment with a referrer
                        if (!await TrySharePaymentWithReferrer(dbContext, output, subRequest, subInfo, requestedTierInfo, user, recipientId, cost, subState, subTx))
                        {
                            await dbContext.SaveAsync();
                        }

                        subState = SubscriptionState.Subscribed;
                    }
                    //append receipt object with what client requested
                    await TxUtils.AppendTxs(transactions, dbContext, subRequest.Columns);

                    var tx = transactions[0];

                    output.TxId = tx.TxId;
                    output.User = tx.From;
                }
            }
            //couldn't process transaction
            if (txRequest == null)
            {
                //fill out response object
                await PopuplateUserResponse(dbContext, subRequest, output, user);
                if (existingSubscription != null)
                {
                    sub = existingSubscription.UserSubscription;
                }
            }
            
            if (subState == SubscriptionState.Subscribed && sub != null)
            {
                var end = output.SubscriptionEndTime = sub.LastSubDate.Value.AddDays(subInfo.Duration);
                //calculate days left
                output.DaysLeft = Math.Ceiling((end.Value - DateTime.Now).TotalDays);
                //setup sub info
                output.UserSubscriptionInfo = await GetUserSubscriptions(dbContext, user)
                    .Where(t => t.SubscriptionTier.SubscriptionId == subInfo.SubscriptionId).FirstOrDefaultAsync();
            }
            return output;
        }

        private static async Task<bool> TrySharePaymentWithReferrer(BitcornContext dbContext,
            SubscriptionResponse output,
            SubRequest subRequest,
            Subscription subInfo,
            SubscriptionTier requestedTierInfo,
            User user,
            int subscriptionPaymentRecipientId,
            decimal cost,
            SubscriptionState previousSubState,
            SubTx subTx)
        {
            //if (previousSubState != SubscriptionState.None) return false;
            //if subscription referrar share is defined and its between 0 and 1
            if (subInfo.ReferrerPercentage != null && subInfo.ReferrerPercentage > 0 && subInfo.ReferrerPercentage <= 1)
            {
                //get the user who received the subscription payment
                var subscriptionPaymentRecipient = await dbContext.JoinUserModels().FirstOrDefaultAsync(u => u.UserId == subscriptionPaymentRecipientId);
                //get subscriber userreferral info
                var userReferral = await dbContext.UserReferral.FirstOrDefaultAsync(r => r.UserId == user.UserId);
                if (userReferral != null && userReferral.ReferralId != 0)
                {
                    //get info of the person who referred the subscriber
                    var referrer = await dbContext.Referrer.FirstOrDefaultAsync(u => u.ReferralId == userReferral.ReferralId);
                    //check if referrer can get rewards
                    if (ReferralUtils.IsValidReferrer(referrer) && referrer.EnableSubscriptionRewards)
                    {
                        //get referrer user info
                        var referrerUser = await dbContext.JoinUserModels().FirstOrDefaultAsync(u => u.UserId == referrer.UserId);
                        if (referrerUser != null && !referrerUser.IsBanned)
                        {
                            if (!subInfo.RestrictReferralRewards || (subInfo.RestrictReferralRewards && (referrerUser.Level == "BEST" 
                                || referrerUser.Level == "BAIT" 
                                || referrerUser.IsAdmin()
                                || referrer.Tier >= 3)))
                            {
                                //calculate amount that will be sent to the referrer
                                var referralShare = cost * subInfo.ReferrerPercentage.Value;
                                //prepare transaction to the referrer
                                var referralShareTx = await TxUtils.PrepareTransaction(subscriptionPaymentRecipient, referrerUser, referralShare, "BITCORNFarms", "$sub referral share", dbContext);
                                //make sure stat tracking values have been initialized
                                if (referrerUser.UserStat.TotalReferralRewardsCorn == null)
                                    referrerUser.UserStat.TotalReferralRewardsCorn = 0;
                                //make sure stat tracking values have been initialized
                                if (referrerUser.UserStat.TotalReferralRewardsUsdt == null)
                                    referrerUser.UserStat.TotalReferralRewardsUsdt = 0;
                                //increment total received corn rewards
                                referrerUser.UserStat.TotalReferralRewardsCorn += referralShare;
                                //inceremnt total received usdt rewards
                                referrerUser.UserStat.TotalReferralRewardsUsdt +=
                                    ((referralShare) * (await ProbitApi.GetCornPriceAsync()));
                                //execute transaction
                                if (await TxUtils.ExecuteTransaction(referralShareTx, dbContext))
                                {
                                    //if transaction was made, log & update ytd total
                                    await ReferralUtils.UpdateYtdTotal(dbContext, referrer, referralShare);
                                    var referralTx = await ReferralUtils.LogReferralTx(dbContext, referrerUser.UserId, referralShare, "$sub referral share");
                                    subTx.ReferralTxId = referralTx.ReferralTxId;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static async Task<bool> IsSubbed(BitcornContext dbContext,User user, string subscriptionName, int? tier)
        {
            if (user == null) return false;
            if (user.IsBanned) return false;

            var query = GetUserSubscriptions(dbContext,user).Join(dbContext.Subscription,
                (UserSubcriptionTierInfo info)=>info.SubscriptionTier.SubscriptionId,
                (Subscription sub)=>sub.SubscriptionId,
                (info,sub)=>new { 
                    info,sub
                }).Where(s=>s.sub.Name.ToLower()==subscriptionName.ToLower());

            if (tier != null)
            {
                return await query.Where(s=>s.info.SubscriptionTier.Tier>=tier.Value).AnyAsync();
            }
            else
            {
                return await query.AnyAsync();
            }
        }

        static async Task PopuplateUserResponse(BitcornContext dbContext, SubRequest subRequest,SubscriptionResponse output, User user)
        {
            var selectableUser = new SelectableUser(user);
            var columns = await UserReflection.GetColumns(dbContext, subRequest.Columns, new int[] {
                            selectableUser.UserId
                          });

            if (columns != null && columns.Count > 0)
            {
                foreach (var column in columns.First().Value)
                {
                    selectableUser.Add(column.Key, column.Value);
                }
            }


            output.User = selectableUser;
        }
    }
}
