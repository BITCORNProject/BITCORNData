﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Platforms;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public RegisterController(BitcornContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("newuser")]
        public async Task<FullUser> RegisterNewUser([FromBody]Auth0User auth0User, [FromQuery] string referral = null)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (auth0User == null) throw new ArgumentNullException();

            var existingUserIdentity = await _dbContext.Auth0Query(auth0User.Auth0Id).Select(u => u.UserIdentity).FirstOrDefaultAsync();

            if (existingUserIdentity?.Auth0Id == auth0User.Auth0Id)
            {
                var user = _dbContext.User.FirstOrDefault(u => u.UserId == existingUserIdentity.UserId);
                var userWallet = _dbContext.UserWallet.FirstOrDefault(u => u.UserId == existingUserIdentity.UserId);
                var userStat = _dbContext.UserStat.FirstOrDefault(u => u.UserId == existingUserIdentity.UserId);
                return BitcornUtils.GetFullUser(user, existingUserIdentity, userWallet, userStat);
            }

            int referralId;
            try
            {
                referralId = Convert.ToInt32(referral);
            }
            catch (Exception e)
            {
                referralId = 0;
            }

            try
            {
                var user = CreateUser(auth0User, referralId);
                _dbContext.User.Add(user);
                if (referral != null)
                {
                    var referrer = await _dbContext.Referrer.FirstOrDefaultAsync(r => r.ReferralId == referralId);
                    if (referrer.YtdTotal < 600 || (referrer.ETag != null && referrer.Key != null))
                    {
                        var referrerUser = await _dbContext.User.FirstOrDefaultAsync(u => u.UserId == referrer.UserId);
                        var referralPayoutTotal = await ReferralUtils.TotalReward(_dbContext, referrer);
                        var referrerRegistrationReward = await TxUtils.SendFromBitcornhub(referrerUser, referralPayoutTotal, "BITCORNFarms", "Registrations reward", _dbContext);
                        var userRegistrationReward = await TxUtils.SendFromBitcornhub(user, referrer.Amount, "BITCORNFarms", "Registrations reward", _dbContext);

                        if (referrerRegistrationReward && userRegistrationReward)
                        {
                            await ReferralUtils.UpdateYtdTotal(_dbContext, referrer, referralPayoutTotal);
                            await ReferralUtils.LogReferralTx(_dbContext, referrer.UserId, referralPayoutTotal, "Registration reward");
                            var referrerStat = await _dbContext.UserStat.FirstOrDefaultAsync(s => s.UserId == referrer.UserId);
                            if (referrerStat != null)
                            {
                                referrerStat.TotalReferrals++;
                                referrerStat.TotalReferralRewardsCorn += referralPayoutTotal;
                                referrerStat.TotalReferralRewardsUsdt += (referralPayoutTotal * Convert.ToDecimal(await ProbitApi.GetCornPriceAsync()));
                            }
                            user.UserReferral.SignupReward = DateTime.Now;
                        }
                    }
                }

                await _dbContext.SaveAsync();

                return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(auth0User));
                throw e;
            }
        }
       
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost]
        public async Task<object> Register([FromBody] RegistrationData registrationData)
        {
            if (registrationData == null) throw new ArgumentNullException("registrationData");
            if (registrationData.Auth0Id == null) throw new ArgumentNullException("registrationData.Auth0Id");
            if (registrationData.PlatformId == null) throw new ArgumentNullException("registrationData.PlatformId");
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            try
            {
                string auth0Id = registrationData.Auth0Id;
                var auth0DbUser = await _dbContext.Auth0Query(auth0Id).FirstOrDefaultAsync();
                var platformId = BitcornUtils.GetPlatformId(registrationData.PlatformId);

                //get all classes that inherit from SocialRegisteration, map to dictionary based on name
                //for example TwitchRegisteration will have key "twitch"
                var registerController = SupportedPlatform.AllocateController(_dbContext, platformId, _configuration);
                if (registerController != null)
                {
                    var result = await registerController.SyncPlatform(registrationData, auth0DbUser, platformId, auth0Id);
                    if (result != null)
                    {
                        //claim transactions etc..
                        await registerController.OnSyncSuccess(platformId);
                    }
                    return result;
                }
                
            }
            catch (Exception e)
            {
                throw new Exception($"registration failed for {registrationData}");
            }
            throw new Exception("HOW THE FUCK DID YOU GET HERE");
        }

        public static User CreateUser(Auth0User auth0User,int referralId)
        {
            var user = new User
            {
                UserIdentity = new UserIdentity
                {
                    Auth0Id = auth0User.Auth0Id,
                    Auth0Nickname = auth0User.Auth0Nickname
                },
                UserWallet = new UserWallet(),
                UserStat = new UserStat(),
                UserReferral = new UserReferral { ReferralId = referralId }
            };

            return user;
        }
    }
}
