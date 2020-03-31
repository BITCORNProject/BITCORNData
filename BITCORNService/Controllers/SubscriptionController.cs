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
            var user = this.GetCachedUser();

            var tx = await SubscriptionUtils.Subscribe(_dbContext, user, subRequest);
            if (tx != null) return tx;

            return StatusCode((int)HttpStatusCode.BadRequest);
        }
        [HttpGet("user")]
        public async Task<ActionResult<object>> User([FromQuery] string platformId, [FromQuery] string subscriptionName = null)
        {
            var id = BitcornUtils.GetPlatformId(platformId);
            var user = await BitcornUtils.GetUserForPlatform(id, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                var subscriptionsQuery = SubscriptionUtils.GetUserSubscriptions(_dbContext, user)
                    .Join(_dbContext.Subscription,
                    (UserSubcriptionTierInfo info)=> info.SubscriptionTier.SubscriptionId,
                    (Subscription sub)=>sub.SubscriptionId,
                    (info,sub)=>new { 
                        userInfo=info,
                        subscriptionInfo=sub
                    }).Where(s=>s.userInfo.UserSubscription.LastSubDate.Value.AddDays(s.subscriptionInfo.Duration)>DateTime.Now);

                if (!string.IsNullOrEmpty(subscriptionName))
                {
                    return await subscriptionsQuery.Where(q=>q.subscriptionInfo.Name.ToLower()==subscriptionName.ToLower()).Select(s => new {
                        daysLeft = (s.userInfo.UserSubscription.LastSubDate.Value.AddDays(s.subscriptionInfo.Duration) - DateTime.Now).TotalDays,
                        tier = s.userInfo.SubscriptionTier.Tier,
                        name = s.subscriptionInfo.Name,
                        description = s.subscriptionInfo.Description
                    }).FirstOrDefaultAsync();
                }
                else
                {
                    return await subscriptionsQuery.Select(s=> new { 
                        daysLeft = (s.userInfo.UserSubscription.LastSubDate.Value.AddDays(s.subscriptionInfo.Duration)-DateTime.Now).TotalDays,
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
      
    }
}