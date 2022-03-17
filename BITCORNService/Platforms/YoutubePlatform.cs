using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BITCORNService.Platforms
{
    public class YoutubePlatform : SupportedPlatform
    {
        public override async Task<PlatformSyncResponse> SyncPlatform(RegistrationData registrationData, User auth0DbUser, PlatformId platformId, string auth0Id)
        {

            var rallyDbUser = await _dbContext.YoutubeQuery(platformId.Id).FirstOrDefaultAsync();
            if (rallyDbUser == null)
            {
                auth0DbUser.UserIdentity.YoutubeId = platformId.Id;
                if (!string.IsNullOrEmpty(SyncName))
                {
                    auth0DbUser.UserIdentity.YoutubeUsername = SyncName.Split("|")[0];

                    auth0DbUser.UserIdentity.YoutubeRefreshToken = SyncName.Split("|")[1];
                }
                await _dbContext.SaveAsync();
                var obj = GetSyncOutput(auth0DbUser, false);
                var rally = new Rally(_configuration);
                rally.SyncYoutube(auth0DbUser.UserIdentity.RallyId, platformId.Id, auth0DbUser.UserIdentity.YoutubeUsername, auth0DbUser.UserIdentity.YoutubeRefreshToken);
                ///user/create/d93cd25c-5ea1-11ec-8847-0a847b2a60cc/382a3191-4f29-464f-baa7-1c586310574c
                return obj;
            }
            else
            {

                var obj = GetSyncOutput(auth0DbUser, false);

                obj.ProfileAlreadySynced = true;
                return obj;
            }
            //return base.SyncPlatform(registrationData, auth0DbUser, platformId, auth0Id);

        }
    }
}
