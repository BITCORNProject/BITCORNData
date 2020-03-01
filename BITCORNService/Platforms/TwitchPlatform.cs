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
    public class TwitchPlatform : SupportedPlatform
    {
        public override async Task<object> SyncPlatform(RegistrationData registrationData, User auth0DbUser, PlatformId platformId, string auth0Id)
        {
            var twitchUser = await TwitchKraken.GetTwitchUser(platformId.Id);

            var twitchDbUser = await _dbContext.TwitchQuery(platformId.Id).FirstOrDefaultAsync();

            if (twitchDbUser != null && twitchDbUser.UserIdentity.Auth0Id == null)
            {
                //   _dbContext.UserIdentity.Remove(auth0DbUser);
                auth0DbUser.UserIdentity.TwitchId = twitchDbUser.UserIdentity.TwitchId;
                MigrateOldIdentity(auth0DbUser.UserIdentity, twitchDbUser.UserIdentity);
                twitchDbUser.UserIdentity.TwitchUsername = twitchUser.display_name;
                twitchDbUser.UserIdentity.Auth0Id = auth0Id;
                twitchDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;

                await MigrateOldProfile(auth0DbUser, twitchDbUser);
                return GetSyncOutput(twitchDbUser, true);
            }
            else if (twitchDbUser == null && auth0DbUser != null)
            {
                auth0DbUser.UserIdentity.TwitchId = platformId.Id;
                auth0DbUser.UserIdentity.TwitchUsername = twitchUser.name;
                await _dbContext.SaveAsync();
                return GetSyncOutput(auth0DbUser, false);
            }
            else if (twitchDbUser != null)
            {
                var e = new Exception($"A login id already exists for this twitch id {platformId.Id}");
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                throw e;
            }
            else
            {
                var e = new Exception(
                    $"Failed to register twitch {platformId.Id} {auth0Id}");
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                throw e;
            }
        }
    }
}
