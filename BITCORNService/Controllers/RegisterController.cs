using System;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public RegisterController(BitcornContext dbContext,IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpPost("newuser")]
        public async Task<FullUser> RegisterNewUser([FromBody]Auth0User auth0User)
        {
            if(auth0User == null) throw new ArgumentNullException();

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
                UserIdentity auth0DbUser = await _dbContext.Auth0Query(auth0Id).Select(u => u.UserIdentity).FirstOrDefaultAsync();
                var platformId = BitcornUtils.GetPlatformId(registrationData.PlatformId);
                switch (platformId.Platform)
                {
                    case "twitch":
                        var twitchUser = await TwitchKraken.GetTwitchUser(platformId.Id);

                        var twitchDbUser = await _dbContext.TwitchQuery(platformId.Id).Select(u => u.UserIdentity).FirstOrDefaultAsync();

                        if (twitchDbUser != null && twitchDbUser.Auth0Id == null)
                        {
                            _dbContext.UserIdentity.Remove(auth0DbUser);
                            twitchDbUser.TwitchUsername = twitchUser.name;
                            twitchDbUser.Auth0Id = auth0Id;
                            twitchDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                            await _dbContext.SaveAsync();

                            await TxUtils.TryClaimTx(platformId, null, _dbContext);
                            return twitchDbUser;
                        }
                        else if (twitchDbUser == null && auth0DbUser != null)
                        {
                            auth0DbUser.TwitchId = platformId.Id;
                            auth0DbUser.TwitchUsername = twitchUser.name;
                            await _dbContext.SaveAsync();

                            await TxUtils.TryClaimTx(platformId, null, _dbContext);
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
                            var discordToken = DiscordApi.GetDiscordBotToken(_configuration);
                            var discordUser = await DiscordApi.GetDiscordUser(discordToken,platformId.Id);

                            var discordDbUser = await _dbContext.DiscordQuery(platformId.Id).Select(u => u.UserIdentity).FirstOrDefaultAsync();
                            
                            if (discordDbUser != null && discordDbUser.Auth0Id == null)
                            {
                                _dbContext.UserIdentity.Remove(auth0DbUser);
                                await _dbContext.SaveAsync();
                                discordDbUser.DiscordUsername = DiscordApi.GetUsernameString(discordUser);
                                discordDbUser.Auth0Id = auth0Id;
                                discordDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                                await _dbContext.SaveAsync();
                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
                                return discordDbUser;
                            }
                            else if (discordDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.DiscordId = platformId.Id;
                                auth0DbUser.DiscordUsername = DiscordApi.GetUsernameString(discordUser);

                                await _dbContext.SaveAsync();

                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
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
                    case "twitter":
                        try
                        {
                            var twitterUser = await TwitterApi.GetTwitterUser(_configuration, platformId.Id);
                            var twitterDbUser = await _dbContext.TwitterQuery(platformId.Id).Select(u => u.UserIdentity).FirstOrDefaultAsync();

                            if (twitterDbUser != null && twitterDbUser.Auth0Id == null)
                            {
                                _dbContext.UserIdentity.Remove(auth0DbUser);
                                twitterDbUser.Auth0Id = auth0Id;
                                twitterDbUser.TwitterUsername = twitterUser.Name;
                                twitterDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                                await _dbContext.SaveAsync();
                                await TxUtils.TryClaimTx(platformId,null,_dbContext);
                                return twitterDbUser;
                            }
                            if (twitterDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.TwitterId = platformId.Id;
                                auth0DbUser.TwitterUsername = twitterUser.Name;
                                await _dbContext.SaveAsync();
                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
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
                            var redditDbUser = await _dbContext.RedditQuery(platformId.Id).Select(u => u.UserIdentity).FirstOrDefaultAsync();

                            if (redditDbUser != null && redditDbUser.Auth0Id == null)
                            {
                                _dbContext.UserIdentity.Remove(auth0DbUser);
                                redditDbUser.Auth0Id = auth0Id;
                                redditDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                                await _dbContext.SaveAsync();
                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
                                return redditDbUser;
                            }
                            else if (redditDbUser == null && auth0DbUser != null)
                            {
                                auth0DbUser.RedditId = platformId.Id;
                                await _dbContext.SaveAsync();

                                await TxUtils.TryClaimTx(platformId, null, _dbContext);
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
