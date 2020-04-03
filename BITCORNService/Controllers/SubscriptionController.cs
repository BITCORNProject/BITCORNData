using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public SubscriptionController(IConfiguration configuration, BitcornContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("new")]
        public async Task<ActionResult<SubscriptionResponse>> New([FromBody] SubRequest subRequest)
        {
            try
            {
                var user = this.GetCachedUser();

                var tx = await SubscriptionUtils.Subscribe(_dbContext, user, subRequest);
                if (tx != null) return tx;

                return StatusCode((int)HttpStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext,e,JsonConvert.SerializeObject(subRequest));
                throw e;
            }
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<AvailableSubscriptionResponse>>> Available([FromQuery] string subscriptionName = null)
        {
            try
            {
                AvailableSubscriptionInfo[] subscriptions = null;
                var query = (from subscriptionTier in _dbContext.SubscriptionTier
                             join subscription in _dbContext.Subscription on subscriptionTier.SubscriptionId equals subscription.SubscriptionId
                             select new AvailableSubscriptionInfo
                             {
                                 SubscriptionTier = subscriptionTier,
                                 Subscription = subscription
                             });

                if (string.IsNullOrEmpty(subscriptionName))
                    subscriptions = await query.ToArrayAsync();
                else
                    subscriptions = await query.Where(s => s.Subscription.Name.ToLower() == subscriptionName.ToLower()).ToArrayAsync();

                var availableSubscriptions = new List<AvailableSubscriptionResponse>();
                var cornUsdt = await ProbitApi.GetCornPriceAsync();
                foreach (var row in subscriptions)
                {
                    var existingEntry = availableSubscriptions.FirstOrDefault(l => l.Subscription.SubscriptionId == row.Subscription.SubscriptionId);
                    if (existingEntry == null)
                    {
                        existingEntry = new AvailableSubscriptionResponse()
                        {
                            Subscription = row.Subscription,
                        };
                        availableSubscriptions.Add(existingEntry);
                    }

                    decimal actualCost = 0;
                    if (row.SubscriptionTier.CostUsdt != null && row.SubscriptionTier.CostUsdt > 0)
                        actualCost = SubscriptionUtils.CalculateUsdtToCornCost(cornUsdt, row.SubscriptionTier);

                    else if (row.SubscriptionTier.CostCorn != null && row.SubscriptionTier.CostCorn > 0)
                        actualCost = row.SubscriptionTier.CostCorn.Value;

                    existingEntry.Tiers.Add(new
                    {
                        tierInfo = row.SubscriptionTier,
                        actualCost
                    });
                }
                return availableSubscriptions;
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, subscriptionName);
                throw e;
            }
        }

        [HttpGet("user")]
        public async Task<ActionResult<object>> GetUserSubscriptions([FromQuery] string platformId, [FromQuery] string subscriptionName = null)
        {
            if (string.IsNullOrEmpty(platformId))
                throw new ArgumentException("platformId");

            try
            {
                var id = BitcornUtils.GetPlatformId(platformId);
                var user = await BitcornUtils.GetUserForPlatform(id, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    var subscriptionsQuery = SubscriptionUtils.GetUserSubscriptions(_dbContext, user)
                        .Join(_dbContext.Subscription,
                        (UserSubcriptionTierInfo info) => info.SubscriptionTier.SubscriptionId,
                        (Subscription sub) => sub.SubscriptionId,
                        (info, sub) => new
                        {
                            userInfo = info,
                            subscriptionInfo = sub
                        }).Where(s => s.userInfo.UserSubscription.LastSubDate.Value.AddDays(s.subscriptionInfo.Duration) > DateTime.Now);

                    if (!string.IsNullOrEmpty(subscriptionName))
                    {
                        return await subscriptionsQuery.Where(q => q.subscriptionInfo.Name.ToLower() == subscriptionName.ToLower()).Select(s => new
                        {
                            daysLeft = (s.userInfo.UserSubscription.LastSubDate.Value.AddDays(s.subscriptionInfo.Duration) - DateTime.Now).TotalDays,
                            tier = s.userInfo.SubscriptionTier.Tier,
                            name = s.subscriptionInfo.Name,
                            description = s.subscriptionInfo.Description
                        }).ToArrayAsync();
                    }
                    else
                    {
                        return await subscriptionsQuery.Select(s => new
                        {
                            daysLeft = (s.userInfo.UserSubscription.LastSubDate.Value.AddDays(s.subscriptionInfo.Duration) - DateTime.Now).TotalDays,
                            tier = s.userInfo.SubscriptionTier.Tier,
                            name = s.subscriptionInfo.Name,
                            description = s.subscriptionInfo.Description
                        }).ToArrayAsync();
                    }
                }
                else
                {
                    return StatusCode(404);
                }
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(new { platformId, subscriptionName}));
                throw e;
            }
        }

        [HttpGet("issubbed")]
        public async Task<ActionResult<bool>> IsSubbed([FromQuery] string platformId, [FromQuery] string subscriptionName, [FromQuery] string subTier = null)
        {
            if (string.IsNullOrEmpty(platformId))
                throw new ArgumentException("platformId");
            if (string.IsNullOrEmpty(subscriptionName))
                throw new ArgumentException("subscriptionName");
            try
            {
                var id = BitcornUtils.GetPlatformId(platformId);
                var user = await BitcornUtils.GetUserForPlatform(id, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    int? tier = null;
                    if (!string.IsNullOrEmpty(subTier))
                    {
                        try
                        {
                            tier = int.Parse(subTier);
                        }
                        catch { }
                    }

                    return await SubscriptionUtils.IsSubbed(_dbContext, user, subscriptionName, tier);
                }
                else
                {
                    return StatusCode(404);
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(new { 
                    subTier,
                    platformId,
                    subscriptionName
                }));
                throw e;
            }
        }

    }
}