﻿using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
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
            return DateTime.Now > user.FarmsSubDate.Value.AddDays(subscription.Duration);
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

        public static async Task<SubscriptionResponse> Subscribe(BitcornContext dbContext, User user, SubRequest subRequest)
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
                    UserSubcriptionTierInfo[] existingSubscriptions = new UserSubcriptionTierInfo[0];
                    if (user != null)
                    {
                        existingSubscriptions = await (from userSubscription in dbContext.UserSubscription
                                                       join subsriptionTier in dbContext.SubscriptionTier
                                                       on userSubscription.SubscriptionTierId equals subsriptionTier.SubscriptionTierId
                                                       select new UserSubcriptionTierInfo
                                                       {
                                                           UserSubscription = userSubscription,
                                                           SubscriptionTier = subsriptionTier
                                                       }).Where(t => t.UserSubscription.UserId == user.UserId && t.SubscriptionTier.SubscriptionId == subInfo.SubscriptionId).ToArrayAsync();
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

                    if (subState != SubscriptionState.Subscribed && subRequest.Amount==requestedTierInfo.Cost)
                    {
                        /*
                        if (subRequest.Amount != requestedTierInfo.Cost)
                        {
                            return null;
                        }*/
                        
                        subRequest.FromUser = user;

                        subRequest.Amount = requestedTierInfo.Cost;
                        var processInfo = await TxUtils.ProcessRequest(subRequest, dbContext);
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

                                sub.FarmsSubDate = DateTime.Now;
                                
                                await dbContext.Database.ExecuteSqlRawAsync(sql.ToString());
                                await dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                                subState = SubscriptionState.Subscribed;
                            }
                            
                            await TxUtils.AppendTxs(transactions, dbContext, subRequest.Columns);
                            
                            var tx= transactions[0];
                            
                            output.TxId = tx.TxId;
                            output.User = tx.From;
                        }
                    }

                    if (subRequest.FromUser == null)
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
                        if (existingSubscription != null)
                        {
                            sub = existingSubscription.UserSubscription;
                        }
                    }

                    if (subState == SubscriptionState.Subscribed&&sub!=null)
                    {
                        output.SubscriptionEndTime = sub.FarmsSubDate.Value.AddDays(subInfo.Duration);
                    }
                 
                }

            }

            return output;
        }
    }
}
