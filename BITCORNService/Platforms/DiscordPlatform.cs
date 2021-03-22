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
    public class DiscordPlatform : SupportedPlatform
    {
        public override async Task<PlatformSyncResponse> SyncPlatform(RegistrationData registrationData, User auth0DbUser, PlatformId platformId, string auth0Id)
        {
            try
            {
                var discordToken = DiscordApi.GetDiscordBotToken(_configuration);
                var discordUser = await DiscordApi.GetDiscordUser(discordToken, platformId.Id);

                var discordDbUser = await _dbContext.DiscordQuery(platformId.Id).FirstOrDefaultAsync();
                var creationTime = discordUser.GetCreatedTime();
                if (discordDbUser != null && discordDbUser.UserIdentity.Auth0Id == null)
                {
                    //_dbContext.UserIdentity.Remove(auth0DbUser);
                    //await _dbContext.SaveAsync();
                    auth0DbUser.UserIdentity.DiscordId = discordDbUser.UserIdentity.DiscordId;
                    MigrateOldIdentity(auth0DbUser.UserIdentity, discordDbUser.UserIdentity);

                    discordDbUser.UserIdentity.DiscordUsername = DiscordApi.GetUsernameString(discordUser);
                    discordDbUser.UserIdentity.Auth0Id = auth0Id;
                    discordDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;
                    await MigrateOldProfile(auth0DbUser, discordDbUser);
                    return GetSyncOutput(creationTime, discordDbUser, true);
                }
                else if (discordDbUser == null && auth0DbUser != null)
                {
                    auth0DbUser.UserIdentity.DiscordId = platformId.Id;
                    auth0DbUser.UserIdentity.DiscordUsername = DiscordApi.GetUsernameString(discordUser);

                    await _dbContext.SaveAsync();
                    return GetSyncOutput(creationTime, auth0DbUser, false);
                }
                else if (discordDbUser?.UserIdentity.Auth0Id != null)
                {
                    var obj = GetSyncOutput(creationTime, discordDbUser, false);
                    obj.ProfileAlreadySynced = true;
                    return obj;
                    /*
                    var e = new Exception($"A login id already exists for this discord id");
                    await BITCORNLogger.LogError(_dbContext, e, $"Auth0Id already exists for user {platformId.Id}");
                    throw e;*/
                }
                else
                {
                    var e = new Exception($"Failed to register discord");
                    await BITCORNLogger.LogError(_dbContext, e, $"Failed to register discord id for user {platformId.Id} {auth0Id}");
                    throw e;
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                throw new Exception($"Failed to add user's discord");
            }

            throw new Exception($"HOW THE FUCK DID YOU GET HERE");
        }
    }
}
