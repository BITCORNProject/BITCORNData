using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BITCORNService.Utils.Models;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        // POST: api/Register
        [HttpPost("twitch")]
        public async Task<UserIdentity> Twitch([FromBody] Auth0TwitchIdentity data)
        {
            try
            {
                var twitchUser = await TwitchKraken.GetTwitchUser(data.TwitchId);

                using (var dbContext = new BitcornContext())
                {
                    var twitchDbUser = await dbContext.TwitchAsync(data.TwitchId);
                    var auth0DbUser = await dbContext.Auth0Async(data.Auth0Id);

                    if (twitchDbUser != null && twitchDbUser.Auth0Id == null)
                    {
                        dbContext.UserIdentity.Remove(auth0DbUser);
                        twitchDbUser.TwitchUsername = twitchUser.name;
                        twitchDbUser.Auth0Id = data.Auth0Id;
                        twitchDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                        await dbContext.SaveAsync();
                        ////////////////////////////////////////////////////////////////
                        // TODO: Add claims
                        return twitchDbUser;
                    }
                    else if (twitchDbUser == null && auth0DbUser != null)
                    {
                        auth0DbUser.TwitchId = data.TwitchId;
                        auth0DbUser.TwitchUsername = twitchUser.name;
                        await dbContext.SaveAsync();
                        return auth0DbUser;
                    }
                    else if (twitchDbUser != null)
                    {
                        var e = new Exception($"A login id already exists for this twitch id {data.TwitchId}");
                        await BITCORNLogger.LogError(e);
                        throw e;
                    }
                    else
                    {
                        var e = new Exception($"Failed to register twitch {data.TwitchId} {data.Auth0Id}");
                        await BITCORNLogger.LogError(e);
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(e);
                throw e;
            }
        }
        [HttpPost("discord")]
        public async Task<UserIdentity> Discord([FromBody] Auth0DiscordIdentity data)
        {
            try
            {
                using (var dbContext = new BitcornContext())
                {
                    var discordDbUser = await dbContext.DiscordAsync(data.DiscordId);
                    var auth0DbUser = await dbContext.Auth0Async(data.Auth0Id);

                    if (discordDbUser != null && discordDbUser.Auth0Id == null)
                    {
                        dbContext.UserIdentity.Remove(auth0DbUser);
                        await dbContext.SaveAsync();
                        discordDbUser.Auth0Id = data.Auth0Id;
                        discordDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                            await dbContext.SaveAsync();
                            ////////////////////////////////////////////////////////////////
                            // TODO: Add registrations claims
                            // await Payments.ScheduledPaymentManager.TryClaim(discordDbUser, _logger, true);
                        return discordDbUser;
                    }
                    else if (discordDbUser == null && auth0DbUser != null)
                    {
                        auth0DbUser.DiscordId = data.DiscordId;
                        await dbContext.SaveAsync();
                        return auth0DbUser;
                    }
                    else if (discordDbUser?.Auth0Id != null)
                    {
                        var e = new Exception($"A login id already exists for this discord id");
                        await BITCORNLogger.LogError(e,$"Auth0Id already exists for user {data.DiscordId}");
                        throw e;
                    }
                    else
                    {
                        var e = new Exception($"Failed to register discord");
                        await BITCORNLogger.LogError(e,$"Failed to register discord id for user {data.DiscordId} {data.Auth0Id}");
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(e);
                throw new Exception($"Failed to add user's discord");
            }

            throw new Exception($"HOW THE FUCK DID YOU GET HERE");
        }
        [HttpPost("twitter")]
        public async Task<UserIdentity> Twitter([FromBody] Auth0TwitterIdentity data)
        {
            try
            {
                using (var dbContext = new BitcornContext())
                {
                    var twitterDbUser = await dbContext.TwitterAsync(data.TwitterId);
                    var auth0DbUser = await dbContext.Auth0Async(data.Auth0Id);


                    if (twitterDbUser != null && twitterDbUser.Auth0Id == null)
                    {
                        dbContext.UserIdentity.Remove(auth0DbUser);
                        twitterDbUser.Auth0Id = data.Auth0Id;
                        twitterDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                        await dbContext.SaveAsync();
                           ///////////////////////////////////////
                           //TODO claim transactions
                           // await Payments.ScheduledPaymentManager.TryClaim(twitterDbUser, _logger, true);
                        return twitterDbUser;
                    }
                    if (twitterDbUser == null && auth0DbUser != null)
                    {
                        auth0DbUser.TwitterId = data.TwitterId;
                        await dbContext.SaveAsync();
                        return auth0DbUser;
                    }
                    if (twitterDbUser?.Auth0Id != null)
                    {
                        var e = new Exception($"Auth0Id already exists for user {data.TwitterId}");
                        await BITCORNLogger.LogError(e);
                        throw e;
                    }
                    var ex = new Exception($"Failed to register twitter id for user {data.TwitterId} {data.Auth0Id}");
                    await BITCORNLogger.LogError(ex);
                    throw ex;
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(e);
                throw e;
            }

            throw new Exception($"HOW THE FUCK DID YOU GET HERE");
        }
        [HttpPost("reddit")]
        public async Task<UserIdentity> Reddit([FromBody] Auth0RedditIdentity data)
        {
            try
            {
                using (var dbContext = new BitcornContext())
                {
                    var redditDbUser = await dbContext.RedditAsync(data.RedditId);
                    var auth0DbUser = await dbContext.Auth0Async(data.Auth0Id);


                    if (redditDbUser != null && redditDbUser.Auth0Id == null)
                    {
                        dbContext.UserIdentity.Remove(auth0DbUser);
                        redditDbUser.Auth0Id = data.Auth0Id;
                        redditDbUser.Auth0Nickname = auth0DbUser.Auth0Nickname;
                        await dbContext.SaveAsync();
                        /////////////////////////////
                        // TODO claim tx
                        // await Payments.ScheduledPaymentManager.TryClaim(redditDbUser, _logger, true);
                        return redditDbUser;
                    }
                    else if (redditDbUser == null && auth0DbUser != null)
                    {
                        auth0DbUser.RedditId = data.RedditId;
                        await dbContext.SaveAsync();
                        return auth0DbUser;
                    }
                    else if (redditDbUser?.Auth0Id != null)
                    {
                        var e = new Exception($"Auth0Id already exists for user {data.RedditId}");
                        await BITCORNLogger.LogError(e);
                        throw e;
                    }
                    else
                    {
                        var e = new Exception($"Failed to register reddit id for user {data.RedditId} {data.RedditId}");
                        await BITCORNLogger.LogError(e);
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(e);
                throw e;
            }

            throw new Exception($"HOW THE FUCK DID YOU GET HERE");
        }
    }
}