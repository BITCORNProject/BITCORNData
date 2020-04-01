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
                var subInfo = await dbContext.Subscription.FirstOrDefaultAsync(s => s.Name.ToLower() == subRequest.SubscriptionName.ToLower());
                var output = new SubscriptionResponse();
                if (subInfo != null)
                {
                    output.RequestedSubscriptionInfo = subInfo;
                    var requestedTierInfo = await dbContext.SubscriptionTier.FirstOrDefaultAsync(t => t.SubscriptionId == subInfo.SubscriptionId && t.Tier == subRequest.Tier);
                    output.RequestedSubscriptionTier = requestedTierInfo;
                    if (requestedTierInfo != null)
                    {
                        decimal cost = 0;
                        if (requestedTierInfo.CostUsdt != null && requestedTierInfo.CostUsdt > 0)
                        {
                            cost = await CalculateUsdtToCornCost(requestedTierInfo);
                        }
                        else if (requestedTierInfo.CostCorn != null && requestedTierInfo.CostCorn > 0)
                        {
                            cost = requestedTierInfo.CostCorn.Value;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid cost setting on subscription tier id:{requestedTierInfo.SubscriptionId}");
                        }
                        output.Cost = cost;
                        UserSubcriptionTierInfo[] existingSubscriptions = new UserSubcriptionTierInfo[0];
                        if (user != null)
                        {
                            existingSubscriptions = await GetUserSubscriptions(dbContext, user)
                                .Where(t => t.SubscriptionTier.SubscriptionId == subInfo.SubscriptionId).ToArrayAsync();
                        }

                        UserSubcriptionTierInfo existingSubscription = null;

                        var subState = SubscriptionState.None;
                        if (existingSubscriptions.Any())
                        {
                            existingSubscription = existingSubscriptions[0];
                            if (subInfo.HasExpired(existingSubscription.UserSubscription))
                                subState = SubscriptionState.Expired;
                            else if (existingSubscription.SubscriptionTier.Tier < requestedTierInfo.Tier)
                                subState = SubscriptionState.TierDown;
                            else subState = SubscriptionState.Subscribed;
                        }

                        UserSubscription sub = null;
                        TxRequest txRequest = null;
                        if (subState != SubscriptionState.Subscribed && subRequest.Amount == cost)
                        {
                            string[] to = new string[1];
                            if (subInfo.OwnerUserId != null)
                            {
                                to[0] = $"userid|{subInfo.OwnerUserId.Value}";
                            }
                            else
                            {
                                to[0] = $"userid|{TxUtils.BitcornHubPK}";
                            }

                            txRequest = new TxRequest(user,cost,subRequest.Platform, "$sub", to);
                            var processInfo = await TxUtils.ProcessRequest(txRequest, dbContext);
                            var transactions = processInfo.Transactions;
                            if (transactions != null && transactions.Length > 0)
                            {
                                StringBuilder sql = new StringBuilder();
                                if (processInfo.WriteTransactionOutput(sql))
                                {

                                    switch (subState)
                                    {
                                        case SubscriptionState.None:
                                            sub = new UserSubscription();
                                            sub.SubscriptionId = subInfo.SubscriptionId;
                                            sub.SubscriptionTierId = requestedTierInfo.SubscriptionTierId;
                                            sub.UserId = user.UserId;
                                            sub.FirstSubDate = DateTime.Now;
                                            dbContext.UserSubscription.Add(sub);
                                            break;
                                        case SubscriptionState.TierDown:
                                        case SubscriptionState.Expired:
                                            existingSubscription.UserSubscription.SubscriptionTierId = requestedTierInfo.SubscriptionTierId;
                                            sub = existingSubscription.UserSubscription;
                                            break;
                                        default:
                                            break;
                                    }

                                    sub.LastSubDate = DateTime.Now;

                                    await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                                    await dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                                    
                                    var subTx = new SubTx();
                                    subTx.UserId = user.UserId;
                                    subTx.SubTxId = transactions[0].TxId.Value;
                                    subTx.UserSubscriptionId = sub.UserSubscriptionId;
                                    dbContext.SubTx.Add(subTx);

                                    await dbContext.SaveAsync();

                                    subState = SubscriptionState.Subscribed;
                                }

                                await TxUtils.AppendTxs(transactions, dbContext, subRequest.Columns);

                                var tx = transactions[0];

                                output.TxId = tx.TxId;
                                output.User = tx.From;
                            }
                        }

                        if (txRequest == null)
                        {
                            await PopuplateUserResponse(dbContext, subRequest, output, user);
                            if (existingSubscription != null)
                            {
                                sub = existingSubscription.UserSubscription;
                            }
                        }

                        if (subState == SubscriptionState.Subscribed && sub != null)
                        {
                            var end = output.SubscriptionEndTime = sub.LastSubDate.Value.AddDays(subInfo.Duration);
                            output.DaysLeft = Math.Ceiling((end.Value - DateTime.Now).TotalDays);
                            output.UserSubscriptionInfo = await GetUserSubscriptions(dbContext, user)
                                .Where(t => t.SubscriptionTier.SubscriptionId == subInfo.SubscriptionId).FirstOrDefaultAsync();
                        }
                    }
                    else
                    {
                        await PopuplateUserResponse(dbContext, subRequest, output, user);
                    }
                }
                else
                {
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
