using System;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.Extensions.Configuration;
using BITCORNService.Utils.DbActions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BITCORNService.Platforms
{
    public class TwitterPlatform : SupportedPlatform
    {
        public override async Task<PlatformSyncResponse> SyncPlatform(RegistrationData registrationData, User auth0DbUser, PlatformId platformId, string auth0Id)
        {
            try
            {
                var twitterUser = await TwitterApi.GetTwitterUser(_configuration, platformId.Id);
                var twitterDbUser = await _dbContext.TwitterQuery(platformId.Id).FirstOrDefaultAsync();

                if (twitterDbUser != null && twitterDbUser.UserIdentity.Auth0Id == null)
                {
                    auth0DbUser.UserIdentity.TwitterId = twitterDbUser.UserIdentity.TwitterId;
                    //_dbContext.UserIdentity.Remove(auth0DbUser);
                    MigrateOldIdentity(auth0DbUser.UserIdentity, twitterDbUser.UserIdentity);
                    twitterDbUser.UserIdentity.Auth0Id = auth0Id;
                    twitterDbUser.UserIdentity.TwitterUsername = twitterUser.ScreenName;
                    twitterDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;
                    await MigrateOldProfile(auth0DbUser, twitterDbUser);
                    return GetSyncOutput(twitterDbUser, true);
                }
                if (twitterDbUser == null && auth0DbUser != null)
                {
                    auth0DbUser.UserIdentity.TwitterId = platformId.Id;
                    auth0DbUser.UserIdentity.TwitterUsername = twitterUser.ScreenName;
                    await _dbContext.SaveAsync(); 
                    return GetSyncOutput(auth0DbUser, false);
                }
                if (twitterDbUser?.UserIdentity.Auth0Id != null)
                {
                    var e = new Exception($"Auth0Id already exists for user {platformId.Id}");
                    await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                    throw e;
                }
                var ex = new Exception($"Failed to register twitter id for user {platformId.Id} {auth0Id}");
                await BITCORNLogger.LogError(_dbContext, ex, JsonConvert.SerializeObject(registrationData));
                throw ex;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                throw e;
            }
            throw new Exception($"HOW THE FUCK DID YOU GET HERE");
        }
    }
}
