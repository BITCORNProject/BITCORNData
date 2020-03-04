using System;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Games;
using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly BitcornGameContext _dbContext;
        private readonly IConfiguration _configuration;
        public RegisterController(BitcornGameContext dbContext,IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("newuser")]
        public async Task<FullUser> RegisterNewUser([FromBody]Auth0User auth0User,[FromQuery] string referral = null)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (auth0User == null) throw new ArgumentNullException();

            var existingUserIdentity = await _dbContext.Auth0Query(auth0User.Auth0Id).Select(u=>u.UserIdentity).FirstOrDefaultAsync();
            
            if (existingUserIdentity?.Auth0Id == auth0User.Auth0Id)
            {
                var user = _dbContext.User.FirstOrDefault(u => u.UserId == existingUserIdentity.UserId);
                var userWallet = _dbContext.UserWallet.FirstOrDefault(u => u.UserId == existingUserIdentity.UserId);
                var userStat = _dbContext.UserStat.FirstOrDefault(u => u.UserId == existingUserIdentity.UserId);
                return BitcornUtils.GetFullUser(user, existingUserIdentity, userWallet, userStat);
            }

            try
            {
                var user = new User
                {
                    UserIdentity = new UserIdentity
                    {
                        Auth0Id = auth0User.Auth0Id, Auth0Nickname = auth0User.Auth0Nickname
                    },
                    UserWallet = new UserWallet(),
                    UserStat = new UserStat(),
                    UserReferral = new UserReferral {ReferralId = 0}


                };
                if (referral != null)
                {
                    var refererId = _dbContext.Referrer
                        .FirstOrDefault(r => r.ReferralId == Convert.ToInt32(referral))?.UserId;
                    var referrerStat = _dbContext.UserStat.FirstOrDefault(s => s.UserId == refererId);
                    user.UserReferral.ReferralId = Convert.ToInt32(referral);

                    var refererWallet = _dbContext.UserWallet.FirstOrDefault(w => w.UserId == refererId);
                    var referrer = _dbContext.Referrer.FirstOrDefault(r => r.UserId == refererId);

                    if (refererWallet != null && referrer != null)
                    {
                        if (referrerStat != null)
                        {
                            referrerStat.TotalReferrals++;
                            referrerStat.TotalReferralRewards += referrer.Amount;
                        }
                        var botWallet = _dbContext.UserWallet.FirstOrDefault(w => w.UserId == 196);
                        if (botWallet != null)
                        {
                            botWallet.Balance -= referrer.Amount;
                        }
                        user.UserReferral.SignupReward = true;
                    }
                }

                _dbContext.User.Add(user);
                await _dbContext.SaveAsync();

                return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(auth0User));
                throw e;
            }
        }
        object GetFullUser(User user,bool isMigration)
        {
            return new { 
                IsMigration = isMigration,
                User = BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat)
            };
        }
        async Task MigrateUser(User delete, User user)
        {
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
        void CopyIdentity(UserIdentity from,UserIdentity to)
        {
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
                switch (platformId.Platform)
                {
                    case "twitch":
                        var twitchUser = await TwitchKraken.GetTwitchUser(platformId.Id);

                        var twitchDbUser = await _dbContext.TwitchQuery(platformId.Id).FirstOrDefaultAsync();

                        if (twitchDbUser != null && twitchDbUser.UserIdentity.Auth0Id == null)
                        {
                            //   _dbContext.UserIdentity.Remove(auth0DbUser);
                            auth0DbUser.UserIdentity.TwitchId = twitchDbUser.UserIdentity.TwitchId;
                            CopyIdentity(auth0DbUser.UserIdentity,twitchDbUser.UserIdentity);
                            twitchDbUser.UserIdentity.TwitchUsername = twitchUser.display_name;
                            twitchDbUser.UserIdentity.Auth0Id = auth0Id;
                            twitchDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;
                           
                            await MigrateUser(auth0DbUser,twitchDbUser);
             

                            await TxUtils.TryClaimTx(platformId, null, _dbContext);
                            await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                            return GetFullUser(twitchDbUser,true);
                        }
                        else if (twitchDbUser == null && auth0DbUser != null)
                        {
                            auth0DbUser.UserIdentity.TwitchId = platformId.Id;
                            auth0DbUser.UserIdentity.TwitchUsername = twitchUser.name;
                            await _dbContext.SaveAsync();

                            await TxUtils.TryClaimTx(platformId, null, _dbContext);
                            await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                            return GetFullUser(auth0DbUser,false);
                        }
                        else if (twitchDbUser != null)
                        {
                            var e = new Exception($"A login id already exists for this twitch id {platformId.Id}");
                            await BITCORNLogger.LogError(_dbContext, e,JsonConvert.SerializeObject(registrationData));
                            throw e;
                        }
                        else
                        {
                            var e = new Exception(
                                $"Failed to register twitch {platformId.Id} {auth0Id}");
                            await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                            throw e;
                        }
                    case "discord":
                        try
                        {
                            var discordToken = DiscordApi.GetDiscordBotToken(_configuration);
                            var discordUser = await DiscordApi.GetDiscordUser(discordToken,platformId.Id);

                            var discordDbUser = await _dbContext.DiscordQuery(platformId.Id).FirstOrDefaultAsync();
                            
                            if (discordDbUser != null && discordDbUser.UserIdentity.Auth0Id == null)
                            {
                                //_dbContext.UserIdentity.Remove(auth0DbUser);
                                //await _dbContext.SaveAsync();
                                auth0DbUser.UserIdentity.DiscordId = discordDbUser.UserIdentity.DiscordId;
                                CopyIdentity(auth0DbUser.UserIdentity,discordDbUser.UserIdentity);
                             
                                discordDbUser.UserIdentity.DiscordUsername = DiscordApi.GetUsernameString(discordUser);
                                discordDbUser.UserIdentity.Auth0Id = auth0Id;
                                discordDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;
                                await MigrateUser(auth0DbUser,discordDbUser);
                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
                                await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                                return GetFullUser(discordDbUser,true);
                            }
                            else if (discordDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.UserIdentity.DiscordId = platformId.Id;
                                auth0DbUser.UserIdentity.DiscordUsername = DiscordApi.GetUsernameString(discordUser);

                                await _dbContext.SaveAsync();

                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
                                await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                                return GetFullUser(auth0DbUser,false);
                            }
                            else if (discordDbUser?.UserIdentity.Auth0Id != null)
                            {
                                var e = new Exception($"A login id already exists for this discord id");
                                await BITCORNLogger.LogError(_dbContext,e, $"Auth0Id already exists for user {platformId.Id}");
                                throw e;
                            }
                            else
                            {
                                var e = new Exception($"Failed to register discord");
                                await BITCORNLogger.LogError(_dbContext,e, $"Failed to register discord id for user {platformId.Id} {auth0Id}");
                                throw e;
                            }
                        }
                        catch (Exception e)
                        {
                            await BITCORNLogger.LogError(_dbContext,e, JsonConvert.SerializeObject(registrationData));
                            throw new Exception($"Failed to add user's discord");
                        }

                        throw new Exception($"HOW THE FUCK DID YOU GET HERE");
                    case "twitter":
                        try
                        {
                            var twitterUser = await TwitterApi.GetTwitterUser(_configuration, platformId.Id);
                            var twitterDbUser = await _dbContext.TwitterQuery(platformId.Id).FirstOrDefaultAsync();

                            if (twitterDbUser != null && twitterDbUser.UserIdentity.Auth0Id == null)
                            {
                                auth0DbUser.UserIdentity.TwitterId = twitterDbUser.UserIdentity.TwitterId;
                                //_dbContext.UserIdentity.Remove(auth0DbUser);
                                CopyIdentity(auth0DbUser.UserIdentity,twitterDbUser.UserIdentity);
                                twitterDbUser.UserIdentity.Auth0Id = auth0Id;
                                twitterDbUser.UserIdentity.TwitterUsername = twitterUser.ScreenName;
                                twitterDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;
                                await MigrateUser(auth0DbUser,twitterDbUser);
                                await TxUtils.TryClaimTx(platformId,null,_dbContext);
                                await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                                return GetFullUser(twitterDbUser,true);
                            }
                            if (twitterDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.UserIdentity.TwitterId = platformId.Id;
                                auth0DbUser.UserIdentity.TwitterUsername = twitterUser.ScreenName;
                                await _dbContext.SaveAsync();
                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
                                await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                                return GetFullUser(auth0DbUser,false);
                            }
                            if (twitterDbUser?.UserIdentity.Auth0Id != null)
                            {
                                var e = new Exception($"Auth0Id already exists for user {platformId.Id}");
                                await BITCORNLogger.LogError(_dbContext,e, JsonConvert.SerializeObject(registrationData));
                                throw e;
                            }
                            var ex = new Exception($"Failed to register twitter id for user {platformId.Id} {auth0Id}");
                            await BITCORNLogger.LogError(_dbContext,ex, JsonConvert.SerializeObject(registrationData));
                            throw ex;
                        }
                        catch (Exception e)
                        {
                            await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(registrationData));
                            throw e;
                        }
                        throw new Exception($"HOW THE FUCK DID YOU GET HERE");
                    case "reddit":
                        try
                        {
                            var redditDbUser = await _dbContext.RedditQuery(platformId.Id).FirstOrDefaultAsync();

                            if (redditDbUser != null && redditDbUser.UserIdentity.Auth0Id == null)
                            {
                                auth0DbUser.UserIdentity.RedditId = redditDbUser.UserIdentity.RedditId;
                                CopyIdentity(auth0DbUser.UserIdentity,redditDbUser.UserIdentity);
                                //_dbContext.UserIdentity.Remove(auth0DbUser);
                                redditDbUser.UserIdentity.Auth0Id = auth0Id;
                                redditDbUser.UserIdentity.Auth0Nickname = auth0DbUser.UserIdentity.Auth0Nickname;
                                await MigrateUser(auth0DbUser,redditDbUser);
                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
                                await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                                return GetFullUser(redditDbUser,true);
                            }
                            else if (redditDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.UserIdentity.RedditId = platformId.Id;
                                await _dbContext.SaveAsync();

                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
                                await ReferralUtils.UpdateReferralSync(_dbContext, platformId);
                                return GetFullUser(auth0DbUser,false);
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
                    default:
                        throw new Exception("Invalid platform provided in the Id");
                }
            }
            catch(Exception e)
            {
                throw new Exception($"registration failed for {registrationData}");
            }
            throw new Exception("HOW THE FUCK DID YOU GET HERE");
        }

    }

    }
