using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Utils.Models;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public RegisterController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("newuser")]
        public async Task<FullUser> RegisterNewUser([FromBody]Auth0User auth0User)
        {
            if(auth0User == null) throw new ArgumentNullException();

            var existingUserIdentity = await _dbContext.Auth0Async(auth0User.Auth0Id);
            
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
                    UserStat = new UserStat()
                };
                _dbContext.User.Add(user);
                await _dbContext.SaveAsync();

                return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPost]
        public async Task<UserIdentity> Register([FromBody] RegistrationData registrationData)
        { 
            if (registrationData == null) throw new ArgumentNullException("registrationData");
            if (registrationData.Auth0Id == null) throw new ArgumentNullException("registrationData.Auth0Id");
            if (registrationData.PlatformId == null) throw new ArgumentNullException("registrationData.PlatformId");

            try
            {
                string auth0Id = registrationData.Auth0Id;
                UserIdentity auth0DbUser = await _dbContext.Auth0Async(auth0Id);
                var platformId = BitcornUtils.GetPlatformId(registrationData.PlatformId);
                switch (platformId.Platform)
                {
                    case "twitch":
                        var twitchUser = await TwitchKraken.GetTwitchUser(platformId.Id);

                        var twitchDbUser = await _dbContext.TwitchAsync(platformId.Id);

                        if (twitchDbUser != null && twitchDbUser.Auth0Id == null)
                        {
                            _dbContext.UserIdentity.Remove(auth0DbUser);
                            twitchDbUser.TwitchUsername = twitchUser.name;
                            twitchDbUser.Auth0Id = auth0Id;
                            twitchDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                            await _dbContext.SaveAsync();
                            ////////////////////////////////////////////////////////////////
                            // TODO: Add claims
                            return twitchDbUser;
                        }
                        else if (twitchDbUser == null && auth0DbUser != null)
                        {
                            auth0DbUser.TwitchId = platformId.Id;
                            auth0DbUser.TwitchUsername = twitchUser.name;
                            await _dbContext.SaveAsync();
                            return auth0DbUser;
                        }
                        else if (twitchDbUser != null)
                        {
                            var e = new Exception($"A login id already exists for this twitch id {platformId.Id}");
                            await BITCORNLogger.LogError(e);
                            throw e;
                        }
                        else
                        {
                            var e = new Exception(
                                $"Failed to register twitch {platformId.Id} {auth0Id}");
                            await BITCORNLogger.LogError(e);
                            throw e;
                        }
                    case "discord":
                        try
                        {
                            var discordDbUser = await _dbContext.DiscordAsync(platformId.Id);

                            if (discordDbUser != null && discordDbUser.Auth0Id == null)
                            {
                                _dbContext.UserIdentity.Remove(auth0DbUser);
                                await _dbContext.SaveAsync();
                                discordDbUser.Auth0Id = auth0Id;
                                discordDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                                await _dbContext.SaveAsync();
                                ////////////////////////////////////////////////////////////////
                                // TODO: Add registrations claims
                                // await Payments.ScheduledPaymentManager.TryClaim(discordDbUser, _logger, true);
                                return discordDbUser;
                            }
                            else if (discordDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.DiscordId = platformId.Id;
                                await _dbContext.SaveAsync();
                                return auth0DbUser;
                            }
                            else if (discordDbUser?.Auth0Id != null)
                            {
                                var e = new Exception($"A login id already exists for this discord id");
                                await BITCORNLogger.LogError(e, $"Auth0Id already exists for user {platformId.Id}");
                                throw e;
                            }
                            else
                            {
                                var e = new Exception($"Failed to register discord");
                                await BITCORNLogger.LogError(e, $"Failed to register discord id for user {platformId.Id} {auth0Id}");
                                await BITCORNLogger.LogError(e, $"Failed to register discord id for user {platformId.Id} {auth0Id}");
                                throw e;
                            }
                        }
                        catch (Exception e)
                        {
                            await BITCORNLogger.LogError(e);
                            throw new Exception($"Failed to add user's discord");
                        }

                        throw new Exception($"HOW THE FUCK DID YOU GET HERE");
                        break;
                    case "twitter":
                        try
                        {
                            var twitterDbUser = await _dbContext.TwitterAsync(platformId.Id);


                            if (twitterDbUser != null && twitterDbUser.Auth0Id == null)
                            {
                                _dbContext.UserIdentity.Remove(auth0DbUser);
                                twitterDbUser.Auth0Id = auth0Id;
                                twitterDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                                await _dbContext.SaveAsync();
                                ///////////////////////////////////////
                                //TODO claim transactions
                                // await Payments.ScheduledPaymentManager.TryClaim(twitterDbUser, _logger, true);
                                return twitterDbUser;
                            }
                            if (twitterDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.TwitterId = platformId.Id;
                                await _dbContext.SaveAsync();
                                return auth0DbUser;
                            }
                            if (twitterDbUser?.Auth0Id != null)
                            {
                                var e = new Exception($"Auth0Id already exists for user {platformId.Id}");
                                await BITCORNLogger.LogError(e);
                                throw e;
                            }
                            var ex = new Exception($"Failed to register twitter id for user {platformId.Id} {auth0Id}");
                            await BITCORNLogger.LogError(ex);
                            throw ex;
                        }
                        catch (Exception e)
                        {
                            await BITCORNLogger.LogError(e);
                            throw e;
                        }
                        throw new Exception($"HOW THE FUCK DID YOU GET HERE");
                    case "reddit":
                        try
                        {
                            var redditDbUser = await _dbContext.RedditAsync(platformId.Id);

                            if (redditDbUser != null && redditDbUser.Auth0Id == null)
                            {
                                _dbContext.UserIdentity.Remove(auth0DbUser);
                                redditDbUser.Auth0Id = auth0Id;
                                redditDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                                await _dbContext.SaveAsync();
                                /////////////////////////////
                                // TODO claim tx
                                // await Payments.ScheduledPaymentManager.TryClaim(redditDbUser, _logger, true);
                                return redditDbUser;
                            }
                            else if (redditDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.RedditId = platformId.Id;
                                await _dbContext.SaveAsync();
                                return auth0DbUser;
                            }
                            else if (redditDbUser?.Auth0Id != null)
                            {
                                var e = new Exception($"Auth0Id already exists for user {platformId.Id}");
                                await BITCORNLogger.LogError(e);
                                throw e;
                            }
                            else
                            {
                                var e = new Exception($"Failed to register reddit id for user {platformId.Id} {platformId.Id}");
                                await BITCORNLogger.LogError(e);
                                throw e;
                            }
                        }
                        catch (Exception e)
                        {
                            await BITCORNLogger.LogError(e);
                            throw e;
                        }

                        throw new Exception($"HOW THE FUCK DID YOU GET HERE");
                    default:
                        break;
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
