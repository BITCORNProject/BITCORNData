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
using RestSharp;

namespace BITCORNService.Platforms
{
    public class TwitchPlatform : SupportedPlatform
    {
        public static async Task<string> RefreshToken(BitcornContext dbContext, UserIdentity user, IConfiguration config)
        {

            var restClient = new RestClient(@"https://id.twitch.tv");

            var request = new RestRequest(Method.POST);
            request.Resource = "oauth2/token";
            request.AddQueryParameter("grant_type", "refresh_token");
            request.AddQueryParameter("refresh_token", user.TwitchRefreshToken);
            request.AddQueryParameter("client_id", config.GetSection("Config").GetSection("TwitchClientIdSub").Value);
            request.AddQueryParameter("client_secret", config.GetSection("Config").GetSection("TwitchClientSecretSub").Value);
            request.AddQueryParameter("scope", "openid channel:read:subscriptions channel:read:redemptions channel:manage:redemptions");

            var response = restClient.Execute(request);
            var twitchRefreshData = JsonConvert.DeserializeObject<TwitchRefreshToken>(response.Content);


            if (!string.IsNullOrWhiteSpace(twitchRefreshData?.AccessToken) && !string.IsNullOrWhiteSpace(twitchRefreshData?.RefreshToken))
            {
                //_twitchToken = twitchRefreshData.AccessToken;
                //_refreshToken = twitchRefreshData.RefreshToken;
                user.TwitchRefreshToken = twitchRefreshData.RefreshToken;
                await dbContext.SaveAsync();
            }

            return twitchRefreshData.AccessToken;

        }
        public override async Task<PlatformSyncResponse> SyncPlatform(RegistrationData registrationData, User auth0DbUser, PlatformId platformId, string auth0Id)
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
                twitchDbUser.UserIdentity.TwitchRefreshToken = registrationData.Token;
                await MigrateOldProfile(auth0DbUser, twitchDbUser);
                return GetSyncOutput(twitchUser.created_at, twitchDbUser, true);
            }
            else if (twitchDbUser == null && auth0DbUser != null)
            {
                auth0DbUser.UserIdentity.TwitchId = platformId.Id;
                auth0DbUser.UserIdentity.TwitchUsername = twitchUser.name;
                auth0DbUser.UserIdentity.TwitchRefreshToken = registrationData.Token;
                await _dbContext.SaveAsync();
                return GetSyncOutput(twitchUser.created_at, auth0DbUser, false);
            }
            else if (twitchDbUser != null)
            {
                /*
                var e = new Exception($"A login id already exists for this twitch id {platformId.Id}");
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                throw e;
                */
                if (auth0DbUser != null && twitchDbUser != null)
                {
                    if (twitchDbUser.UserIdentity.Auth0Id == auth0DbUser.UserIdentity.Auth0Id && !string.IsNullOrEmpty(registrationData.Token))
                    {
                        if (twitchDbUser.UserIdentity.TwitchId == auth0DbUser.UserIdentity.TwitchId)
                        {
                            auth0DbUser.UserIdentity.TwitchRefreshToken = registrationData.Token;
                            await _dbContext.SaveAsync();
                            return GetSyncOutput(twitchUser.created_at, twitchDbUser, false);

                        }
                    }
                }

                var obj = GetSyncOutput(twitchUser.created_at, twitchDbUser, false);
            
                obj.ProfileAlreadySynced = true;
                return obj;
            
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
