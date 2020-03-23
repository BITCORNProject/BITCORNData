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
    public class RedditPlatform : SupportedPlatform
    {
        public override async Task<PlatformSyncResponse> SyncPlatform(RegistrationData registrationData, User auth0DbUser, PlatformId platformId, string auth0Id)
        {
            try
            {
                var redditDbUser = await _dbContext.RedditQuery(platformId.Id).FirstOrDefaultAsync();

                if (redditDbUser != null && redditDbUser.UserIdentity.Auth0Id == null)
                {
                    auth0DbUser.UserIdentity.RedditId = redditDbUser.UserIdentity.RedditId;
                    MigrateOldIdentity(auth0DbUser.UserIdentity, redditDbUser.UserIdentity);
                    //_dbContext.UserIdentity.Remove(auth0DbUser);
                    redditDbUser.UserIdentity.Auth0Id = auth0Id;
                    redditDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;
                    await MigrateOldProfile(auth0DbUser, redditDbUser);
                    return GetSyncOutput(redditDbUser, true);
                }
                else if (redditDbUser == null && auth0DbUser != null)
                {
                    auth0DbUser.UserIdentity.RedditId = platformId.Id;
                    await _dbContext.SaveAsync();
                    return GetSyncOutput(auth0DbUser, false);
                }
                else if (redditDbUser?.UserIdentity.Auth0Id != null)
                {
                    var e = new Exception($"Auth0Id already exists for user {platformId.Id}");
                    await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                    throw e;
                }
                else
                {
                    var e = new Exception($"Failed to register reddit id for user {platformId.Id} {platformId.Id}");
                    await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                    throw e;
                }
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
