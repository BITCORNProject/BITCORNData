using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BITCORNService.Platforms
{
    public class SupportedPlatform
    {
        protected BitcornContext _dbContext;
        protected IConfiguration _configuration;

        static Dictionary<string, Type> _supportedPlatforms;

        //TODO: user this for platform queries so to add a new platform to the system, you only need to implement SupportedPlatform
        public static SupportedPlatform AllocateQueryController(BitcornContext dbContext, PlatformId platformId)
        {
            return AllocateController(dbContext, platformId, null);
        }

        public static SupportedPlatform AllocateController(BitcornContext dbContext, PlatformId platformId, IConfiguration configuration)
        {
            var registerationControllers = SupportedPlatform.GetSupportedPlatforms();
            //try to find registeration controller from mapped out controllers
            if (registerationControllers.TryGetValue(platformId.Platform, out Type registerControllerType))
            {
                //create instance from type
                var registerController = (SupportedPlatform)Activator.CreateInstance(registerControllerType);
                //setup dependencies
                registerController.Setup(dbContext, configuration);
                return registerController;
            }
            else
            {
                throw new ArgumentException($"Unsupported platform:{platformId.Platform}");
            }
        }

        public static Dictionary<string, Type> GetSupportedPlatforms()
        {
            if (_supportedPlatforms == null||_supportedPlatforms.Count==0)
            {
                _supportedPlatforms = Assembly.GetExecutingAssembly().GetTypes()
                          .Where(m => m.IsSubclassOf(typeof(SupportedPlatform)) && m != typeof(SupportedPlatform))
                          .ToDictionary(m => m.Name.Replace("Platform", "").ToLower(), m => m);
            }
            return _supportedPlatforms;
        }

        public void Setup(BitcornContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _configuration = config;
        }
        //cannot be abstract because cannot create instance of SocialRegisteration then..
        public virtual async Task<PlatformSyncResponse> SyncPlatform(RegistrationData registrationData, User auth0DbUser, PlatformId platformId, string auth0Id)
        {
            throw new NotImplementedException();
        }

        public virtual async Task OnSyncSuccess(DateTime? socialAccountCreationDate, PlatformId platformId)
        {
            await TxUtils.TryClaimTx(platformId, null, _dbContext);
            var key = $"{platformId.Platform}|{platformId.Id}";

            if (!(await _dbContext.SocialIdentity.AnyAsync(s => s.PlatformId == key)))
            {
                if (socialAccountCreationDate != null && DateTime.Now > socialAccountCreationDate.Value.AddDays(7))
                {
                    await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                }

                _dbContext.SocialIdentity.Add(new SocialIdentity()
                {
                    PlatformId = key,
                    Timestamp = DateTime.Now
                });
                await _dbContext.SaveAsync();

            }

        }
        protected PlatformSyncResponse GetSyncOutput(User user, bool isMigration)
        {
            return new PlatformSyncResponse()
            {
                IsMigration = isMigration,
                User = BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat),
              
            };
        }
        protected PlatformSyncResponse GetSyncOutput(DateTime? socialCreationTime, User user, bool isMigration)
        {
            return new PlatformSyncResponse()
            {
                IsMigration = isMigration,
                User = BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat),
                SocialCreationTime = socialCreationTime
            };
        }
        /// <summary>
        /// method to migrate pre auth0 user sync info
        /// </summary>
        protected void MigrateOldIdentity(UserIdentity from, UserIdentity to)
        {

            //NOTE: this is only called for pre auth0 user profiles
            to.Auth0Id = from.Auth0Id;
            to.Auth0Nickname = from.Auth0Nickname;
            if (!string.IsNullOrEmpty(from.DiscordId))
            {
                to.DiscordId = from.DiscordId;
                to.DiscordUsername = from.DiscordUsername;
            }
            if (!string.IsNullOrEmpty(from.RedditId))
            {
                to.RedditId = from.RedditId;
            }
            if (!string.IsNullOrEmpty(from.TwitchId))
            {
                to.TwitchId = from.TwitchId;
                to.TwitchUsername = from.TwitchUsername;
            }
            if (!string.IsNullOrEmpty(from.TwitterId))
            {
                to.TwitterId = from.TwitterId;
                to.TwitterUsername = from.TwitterUsername;
            }
        }
        /// <summary>
        /// method to migrate pre auth0 user into current system, this is never called if user has registered with auth0 
        /// </summary>
        protected async Task MigrateOldProfile(User delete, User user)
        {
            //NOTE: this is only called for pre auth0 user profiles
            user.Avatar = delete.Avatar;
            user.IsBanned = delete.IsBanned;
            user.Level = delete.Level;
            user.SubTier = delete.SubTier;

            user.UserStat.EarnedIdle += delete.UserStat.EarnedIdle;
            user.UserStat.AmountOfRainsSent += delete.UserStat.AmountOfRainsSent;
            user.UserStat.AmountOfRainsReceived += delete.UserStat.AmountOfRainsReceived;
            user.UserStat.TotalReceivedBitcornRains += delete.UserStat.TotalReceivedBitcornRains;
            user.UserStat.TotalSentBitcornViaRains += delete.UserStat.TotalSentBitcornViaRains;
            user.UserStat.AmountOfTipsSent += delete.UserStat.AmountOfTipsSent;
            user.UserStat.AmountOfTipsReceived += delete.UserStat.AmountOfTipsReceived;
            user.UserStat.TotalReceivedBitcornTips += delete.UserStat.TotalReceivedBitcornTips;
            user.UserStat.TotalSentBitcornViaTips += delete.UserStat.TotalSentBitcornViaTips;
            user.UserStat.LargestSentBitcornRain += delete.UserStat.LargestSentBitcornRain;
            user.UserStat.LargestReceivedBitcornRain += delete.UserStat.LargestReceivedBitcornRain;
            user.UserStat.LargestSentBitcornTip += delete.UserStat.LargestSentBitcornTip;
            user.UserStat.LargestReceivedBitcornTip += delete.UserStat.LargestReceivedBitcornTip;

            user.UserWallet.Balance += delete.UserWallet.Balance;
            if (!string.IsNullOrEmpty(delete.UserWallet.CornAddy))
            {
                user.UserWallet.CornAddy = delete.UserWallet.CornAddy;
                user.UserWallet.WalletServer = delete.UserWallet.WalletServer;
            }
            _dbContext.Remove(delete.UserWallet);
            _dbContext.Remove(delete.UserIdentity);
            _dbContext.Remove(delete.UserStat);
            _dbContext.User.Remove(delete);
            await _dbContext.Database.ExecuteSqlRawAsync($" UPDATE [{nameof(CornTx)}] SET [{nameof(CornTx.SenderId)}] = {user.UserId} WHERE [{nameof(CornTx.SenderId)}] = {delete.UserId}");
            await _dbContext.Database.ExecuteSqlRawAsync($" UPDATE [{nameof(CornTx)}] SET [{nameof(CornTx.ReceiverId)}] = {user.UserId} WHERE [{nameof(CornTx.ReceiverId)}] = {delete.UserId}");

            await _dbContext.SaveAsync();
        }
    }
}
