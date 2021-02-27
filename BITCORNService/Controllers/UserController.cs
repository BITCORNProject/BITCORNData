using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.CommentUtils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Twitch;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private IConfiguration _config;

        public UserController(IConfiguration config, BitcornContext dbContext)
        {
            _dbContext = dbContext;
            _config = config;
        }

        // POST: api/User   
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<FullUser>> Post([FromRoute] string id)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
            }
            else
            {
                return StatusCode(404);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]

        [HttpGet("{id}/nicknames")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> NickNames([FromRoute] string id, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                string str = "";
                if (user.IsBanned)
                {
                    str += " (BANNED)";
                }
                return new
                {
                    auth0Nickname = user.UserIdentity.Auth0Nickname + str

                };
            }
            else
            {
                return StatusCode(404);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/socialmediaconnections")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialMediaConnections>> GetSocialMediaConnections([FromRoute] string id, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                return SocialMediaConnections.FromUser(user);

            }
            else
            {
                return StatusCode(404);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]

        [HttpGet("{id}/socialmetrics")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> GetSocialMetrics([FromRoute] string id, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                Dictionary<string, object> output = new Dictionary<string, object>();
                BitcornUtils.AppendUserOutput(output, new Type[] { typeof(decimal?), typeof(int?) }, user.UserStat);
                return output;
            }
            else
            {
                return StatusCode(404);
            }
        }

        [Authorize(Policy = AuthScopes.ReadUser)]
        [HttpGet("userid/{id}")]
        public async Task<ActionResult<int>> UserId(string id)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                return user.UserId;
            }
            return StatusCode(404);
        }


        [Authorize(Policy = AuthScopes.ReadTransaction)]
        [HttpGet("transactions/{id}/{offset}/{amount}/{txTypes}")]
        public async Task<ActionResult<object>> Transactions(string id, int offset, int amount, string txTypes = null)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                string[] txTypesArr = null;
                if (!string.IsNullOrEmpty(txTypes)) txTypesArr = txTypes.Split(" ");

                return await Utils.Stats.CornTxUtils.ListTransactions(_dbContext, user.UserId, offset, amount, txTypesArr);
            }
            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}/[action]")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<FullUserAndReferrer>> FullUser([FromRoute] string id)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                var referral = _dbContext.Referrer.FirstOrDefault(r => r.UserId == user.UserId);
                //
                return BitcornUtils.GetFullUserAndReferer(user, user.UserIdentity, user.UserWallet, user.UserStat, user.UserReferral, referral);
            }
            else
            {
                return StatusCode(404);
            }
        }
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/findbyusername/{username}")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> FindUsersByName([FromRoute] string id, [FromRoute] string username)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                return await CommentUtils.FindUsersByName(_dbContext, username).Select((u) => new
                {
                    NickName = u.Auth0Nickname,
                    Auth0Id = u.Auth0Id
                }).ToArrayAsync();
            }
            else
            {
                return StatusCode(404);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("livestreams")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<string[]>> GetLivestreams()
        {
            var streams = await _dbContext.GetLivestreams().Where(e => e.Stream.Enabled).ToArrayAsync();
            List<string> channels = new List<string>();
            foreach (var stream in streams)
            {
                if (!string.IsNullOrEmpty(stream.UserIdentity.TwitchId))
                {
                    channels.Add(stream.UserIdentity.TwitchUsername);
                }

            }

            return channels.ToArray();
        }

        class PublicLivestreamsResponse
        {
            public int AmountOfRainsSent { get; set; }
            public int AmountOfTipsSent { get; set; }
            public string TwitchId { get; set; }
            public string Auth0Id { get; set; }
            public decimal TotalSentBitcornViaRains { get; set; }
            public decimal TotalSentBitcornViaTips { get; set; }
            public decimal Tier3IdlePerMinute { get; set; }
            public decimal Tier2IdlePerMinute { get; set; }
            public decimal Tier1IdlePerMinute { get; set; }
            public bool IsPartner { get; set; }
            public bool IrcPayments { get; set; }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("publiclivestreams")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object[]>> GetPublicLivestreams()
        {
            var streams = await _dbContext.GetLivestreams().Where(e => e.Stream.Enabled && e.Stream.Public).ToArrayAsync();
            var channels = new List<PublicLivestreamsResponse>();
            foreach (var entry in streams)
            {
                if (!string.IsNullOrEmpty(entry.UserIdentity.TwitchId))
                {
                    channels.Add(new PublicLivestreamsResponse
                    {
                        AmountOfRainsSent = entry.Stream.AmountOfRainsSent,
                        AmountOfTipsSent = entry.Stream.AmountOfTipsSent,
                        TwitchId = entry.UserIdentity.TwitchId,
                        TotalSentBitcornViaRains = entry.Stream.TotalSentBitcornViaRains,
                        TotalSentBitcornViaTips = entry.Stream.TotalSentBitcornViaTips,
                        IrcPayments = entry.Stream.IrcEventPayments,
                        Tier3IdlePerMinute = entry.Stream.Tier3IdlePerMinute,
                        Tier1IdlePerMinute = entry.Stream.Tier1IdlePerMinute,
                        Tier2IdlePerMinute = entry.Stream.Tier2IdlePerMinute,
                        IsPartner = entry.Stream.BitcornhubFunded,
                        Auth0Id = entry.UserIdentity.Auth0Id,
                    });
                    //channels.Add(stream.UserIdentity.TwitchUsername);
                }

            }

            return channels.OrderByDescending(x => x.AmountOfRainsSent).ToArray();
        }

        public class UpdateUserRefreshTokenRequest
        {
            public RefreshTokenUpdate[] Tokens { get; set; }
            public class RefreshTokenUpdate
            {
                public string IrcTarget { get; set; }
                public string RefreshToken { get; set; }

            }
        }
 
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("livestreams/updatetoken")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> UpdateToken([FromBody] UpdateUserRefreshTokenRequest r)
        {
            var request = r.Tokens;

            if (request.Where(x => !string.IsNullOrEmpty(x.IrcTarget)).Count() == 0)
            {
                return StatusCode(400);
            }

            var userIds = request.Where(x => !string.IsNullOrEmpty(x.RefreshToken)).Select(x=>x.IrcTarget).ToArray();

            var userIdentities = await _dbContext.UserIdentity.Where(u => userIds.Contains(u.TwitchId)).ToDictionaryAsync(x => x.TwitchId, x => x);
            int changeCount = 0;
            for (int i = 0; i < request.Length; i++)
            {
                var req = request[i];
                if (userIdentities.TryGetValue(req.IrcTarget, out var identity))
                {
                    if (!string.IsNullOrEmpty(identity.TwitchRefreshToken))
                    {
                        identity.TwitchRefreshToken = req.RefreshToken;
                        changeCount++;
                    }
                }
            }

            if (changeCount > 0)
            {
                await _dbContext.SaveAsync();
                return new
                {
                    success = true
                };
            }
            /*
            if (userIdentities != null)
            {
                if (!string.IsNullOrEmpty(userIdentities.TwitchRefreshToken))
                {
                    userIdentities.TwitchRefreshToken = request.RefreshToken;
                    await _dbContext.SaveAsync();
                    return new
                    {
                        success = true
                    }; ;
                }
            }*/

            return new
            {
                success = false
            }; ;

        }

        object GetLivestreamSettingsForUser(UserIdentity userIdentity, UserLivestream stream)
        {
            return new
            {
                MinRainAmount = stream.MinRainAmount,
                MinTipAmount = stream.MinTipAmount,
                RainAlgorithm = stream.RainAlgorithm,
                IrcTarget = userIdentity.TwitchId,//stream.Stream.IrcTarget,
                TxMessages = stream.TxMessages,
                TxCooldownPerUser = stream.TxCooldownPerUser,
                EnableTransactions = stream.EnableTransactions,
                IrcEventPayments = stream.IrcEventPayments,
                BitcornhubFunded = stream.BitcornhubFunded,
                BitcornPerBit = stream.BitcornPerBit,
                BitcornPerDonation = stream.BitcornPerDonation,
                TwitchRefreshToken = userIdentity.TwitchRefreshToken,
                BitcornPerChannelpointsRedemption = stream.BitcornPerChannelpointsRedemption,
                EnableChannelpoints = stream.EnableChannelpoints


            };
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("livestreams/settings")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object[]>> GetLivestreamSettings()
        {
            var streams = await _dbContext.GetLivestreams().Where(e => e.Stream.Enabled).ToArrayAsync();
            List<object> output = new List<object>();
            foreach (var entry in streams)
            {
                if (!string.IsNullOrEmpty(entry.UserIdentity.TwitchId))
                {
                    output.Add(GetLivestreamSettingsForUser(entry.UserIdentity, entry.Stream));
                    //channels.Add(stream.UserIdentity.TwitchUsername);
                }

            }

            return output.ToArray();
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/{name}/toid")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> NameToId([FromRoute] string id, [FromRoute] string name)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var nameLower = name.ToLower();
                UserIdentity result = null;
                if (user.IsAdmin())
                {
                    var nameSplit = name.Split("|");
                    //string nameSrc = null;
                    if (nameSplit.Length > 1)
                    {


                        var nameType = nameSplit[0];
                        if (nameType != "auth0")
                        {
                            nameLower = nameSplit[1].ToLower();
                        }

                        if (nameType == "twitch")
                            result = await _dbContext.UserIdentity.Where(x => x.TwitchUsername.ToLower() == nameLower).FirstOrDefaultAsync();
                        else if (nameType == "discord")
                            result = await _dbContext.UserIdentity.Where(x => x.DiscordUsername.ToLower() == nameLower).FirstOrDefaultAsync();
                        else if (nameType == "twitter")
                            result = await _dbContext.UserIdentity.Where(x => x.TwitterUsername.ToLower() == nameLower).FirstOrDefaultAsync();
                        else if (nameType == "reddit")
                            result = await _dbContext.UserIdentity.Where(x => x.RedditId.ToLower() == nameLower).FirstOrDefaultAsync();
                        else
                        {
                            result = await _dbContext.UserIdentity.Where(x => x.Auth0Nickname.ToLower() == nameLower).FirstOrDefaultAsync();

                        }
                    }
                    else
                    {
                        result = await _dbContext.UserIdentity.Where(x => x.Auth0Nickname.ToLower() == nameLower).FirstOrDefaultAsync();

                    }
                }
                else
                {
                    result = await _dbContext.UserIdentity.Where(x => x.Auth0Nickname.ToLower() == nameLower).FirstOrDefaultAsync();

                }

                if (result != null)
                {
                    return new
                    {
                        auth0Id = result.Auth0Id,
                        success = true
                    };

                }
                else
                {
                    return new
                    {
                        auth0Id = string.Empty,
                        success = false
                    };

                }
                /*
                */
            }
            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/publiclivestream")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> GetPublicLivestream([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var liveStream = await _dbContext.UserLivestream.Where(x => x.UserId == user.UserId).FirstOrDefaultAsync();
                if (liveStream != null && liveStream.Public && !string.IsNullOrEmpty(user.UserIdentity.TwitchId))
                {
                    return new
                    {
                        TwitchId = user.UserIdentity.TwitchId,
                        TwitchUsername = user.UserIdentity.TwitchUsername,
                        TotalSentBitcornViaRains = liveStream.TotalSentBitcornViaRains,
                        TotalSentBitcornViaTips = liveStream.TotalSentBitcornViaTips,
                        AmountOfRainsSent = liveStream.AmountOfRainsSent,
                        AmountOfTipsSent = liveStream.AmountOfTipsSent
                    };
                }
            }

            return new
            {
                TwitchId = "",
                TwitchUsername = "",
                TotalSentBitcornViaRains = 0,
                TotalSentBitcornViaTips = 0,
                AmountOfRainsSent = 0,
                AmountOfTipsSent = 0
            };
            //return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/livestream")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<UserLivestream>> GetLivestream([FromRoute] string id)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                var stream = await _dbContext.GetLivestreams().Where(e => e.UserId == user.UserId).FirstOrDefaultAsync();
                if (stream != null)
                    return stream.Stream;

            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpGet("accesslocked")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public ActionResult CheckAccessLock()
        {
            return StatusCode(200);
        }



        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/setlivestream")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> SetLivestream([FromRoute] string id, [FromBody] SetLivestreamBody body)
        {
            /*
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            */
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    var liveStream = await _dbContext.UserLivestream.FirstOrDefaultAsync(u => u.UserId == user.UserId);
                    if (liveStream == null)
                    {
                        liveStream = new UserLivestream()
                        {
                            Enabled = body.Enabled,
                            UserId = user.UserId,
                            Public = body.Public,
                            EnableTransactions = body.EnableTransactions,
                            IrcTarget = "#" + user.UserIdentity.TwitchUsername,
                            TxCooldownPerUser = body.TxCooldownPerUser,
                            RainAlgorithm = body.RainAlgorithm,
                            MinRainAmount = body.MinRainAmount,
                            MinTipAmount = body.MinTipAmount,
                            TxMessages = body.TxMessages,
                            IrcEventPayments = body.IrcEventPayments,
                            BitcornPerBit = 0.1m,
                            BitcornPerDonation = 1,
                            Tier1IdlePerMinute = 0.1m,
                            Tier2IdlePerMinute = 0.25m,
                            Tier3IdlePerMinute = 1m,
                            BitcornPerChannelpointsRedemption = .1m,
                            EnableChannelpoints = body.EnableChannelpoints


                        };

                        _dbContext.UserLivestream.Add(liveStream);
                        await _dbContext.SaveAsync();
                    }
                    else
                    {
                        bool changes = false;
                        if (body.Enabled != liveStream.Enabled)
                        {
                            changes = true;
                            liveStream.Enabled = body.Enabled;
                        }

                        if (body.Public != liveStream.Public)
                        {
                            liveStream.Public = body.Public;
                            changes = true;
                        }

                        if (body.EnableTransactions != liveStream.EnableTransactions)
                        {
                            liveStream.EnableTransactions = body.EnableTransactions;
                            changes = true;
                        }
                        /*
                        if(user.UserIdentity.TwitchId =! liveStream.IrcTarget)
                        {
                            liveStream.IrcTarget = user.UserI
                        }*/

                        if ("#" + user.UserIdentity.TwitchUsername != liveStream.IrcTarget)
                        {
                            liveStream.IrcTarget = "#" + user.UserIdentity.TwitchUsername;
                            changes = true;
                        }

                        if (body.MinRainAmount != liveStream.MinRainAmount)
                        {
                            liveStream.MinRainAmount = body.MinRainAmount;
                            if (liveStream.MinRainAmount <= 0)
                            {
                                liveStream.MinRainAmount = 1;
                            }
                            changes = true;
                        }

                        if (body.MinTipAmount != liveStream.MinTipAmount)
                        {
                            liveStream.MinTipAmount = body.MinTipAmount;
                            if (liveStream.MinTipAmount <= 0)
                            {
                                liveStream.MinTipAmount = 1;
                            }

                            changes = true;
                        }

                        if (body.TxCooldownPerUser != liveStream.TxCooldownPerUser)
                        {
                            liveStream.TxCooldownPerUser = body.TxCooldownPerUser;
                            if (liveStream.TxCooldownPerUser <= 0)
                            {
                                liveStream.TxCooldownPerUser = 0;
                            }
                            changes = true;
                        }


                        if (body.RainAlgorithm != liveStream.RainAlgorithm)
                        {
                            liveStream.RainAlgorithm = body.RainAlgorithm;
                            if (liveStream.RainAlgorithm < 0)
                            {
                                liveStream.RainAlgorithm = 0;
                            }

                            if (liveStream.RainAlgorithm > 1)
                            {
                                liveStream.RainAlgorithm = 1;
                            }
                            changes = true;
                        }

                        if (body.TxMessages != liveStream.TxMessages)
                        {
                            liveStream.TxMessages = body.TxMessages;
                            changes = true;
                        }

                        if (body.IrcEventPayments != liveStream.IrcEventPayments)
                        {
                            liveStream.IrcEventPayments = body.IrcEventPayments;
                            changes = true;
                        }

                        if (body.Tier1IdlePerMinute != liveStream.Tier1IdlePerMinute)
                        {
                            liveStream.Tier1IdlePerMinute = body.Tier1IdlePerMinute;
                            if (liveStream.Tier1IdlePerMinute <= 0)
                                liveStream.Tier1IdlePerMinute = 0.1m;

                            if (liveStream.Tier1IdlePerMinute > 1000) liveStream.Tier1IdlePerMinute = 1000;

                            changes = true;
                        }

                        if (body.Tier2IdlePerMinute != liveStream.Tier2IdlePerMinute)
                        {
                            liveStream.Tier2IdlePerMinute = body.Tier2IdlePerMinute;
                            if (liveStream.Tier2IdlePerMinute <= 0)
                                liveStream.Tier2IdlePerMinute = 0.1m;

                            if (liveStream.Tier2IdlePerMinute > 1000) liveStream.Tier2IdlePerMinute = 1000;
                            changes = true;
                        }

                        if (body.Tier3IdlePerMinute != liveStream.Tier3IdlePerMinute)
                        {
                            liveStream.Tier3IdlePerMinute = body.Tier3IdlePerMinute;
                            if (liveStream.Tier3IdlePerMinute <= 0)
                                liveStream.Tier3IdlePerMinute = 0.3m;
                            if (liveStream.Tier3IdlePerMinute > 1000) liveStream.Tier3IdlePerMinute = 1000;
                            changes = true;
                        }

                        if (body.BitcornPerBit != liveStream.BitcornPerBit)
                        {
                            liveStream.BitcornPerBit = body.BitcornPerBit;
                            if (liveStream.BitcornPerBit <= 0)
                                liveStream.BitcornPerBit = 0.1m;
                            changes = true;
                        }


                        if (body.BitcornPerDonation != liveStream.BitcornPerDonation)
                        {
                            liveStream.BitcornPerDonation = body.BitcornPerDonation;
                            if (liveStream.BitcornPerDonation <= 0)
                                liveStream.BitcornPerDonation = 0.1m;
                            changes = true;
                        }

                        if (body.EnableChannelpoints != liveStream.EnableChannelpoints)
                        {
                            liveStream.EnableChannelpoints = body.EnableChannelpoints;
                            changes = true;
                        }

                        if (body.BitcornPerChannelpointsRedemption != liveStream.BitcornPerChannelpointsRedemption)
                        {
                            liveStream.BitcornPerChannelpointsRedemption = body.BitcornPerChannelpointsRedemption;
                            if (liveStream.BitcornPerChannelpointsRedemption <= .1m)
                            {
                                liveStream.BitcornPerChannelpointsRedemption = .1m;
                            }
                            changes = true;
                        }

                        if (changes)
                        {
                            await _dbContext.SaveAsync();
                            await WebSocketsController.TryBroadcastToBitcornhub("update-livestream-settings", 
                                GetLivestreamSettingsForUser(user.UserIdentity, liveStream));
                        }



                    }

                    return liveStream;
                }
                else
                {
                    return StatusCode(404);
                }
            }
            catch (Exception ex)
            {
                throw ex;
                //return StatusCode(404);

            }
        }
        //  

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("notifications")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> GetNotifications([FromBody] BulkAuth0Request body)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            var ids = body.Ids;
            var users = await _dbContext.UserIdentity.Where(x => ids.Contains(x.Auth0Id)).ToArrayAsync();
            var userIds = users.Select(x => x.UserId).ToArray();
            var auth0IdDict = users.ToDictionary(x => x.UserId, x => x.Auth0Id);
            var result = await CommentUtils.GetNotifications(_dbContext, userIds);
            var output = new Dictionary<string, List<CommentUtils.TagSelect>>();
            foreach (var item in auth0IdDict)
            {
                output.Add(item.Value, new List<CommentUtils.TagSelect>());
            }

            for (int i = 0; i < result.Length; i++)
            {
                var auth0Id = auth0IdDict[result[i].TaggedId];
                if (!output.TryGetValue(auth0Id, out var list))
                {
                    list = new List<CommentUtils.TagSelect>();
                    output.Add(auth0Id, list);
                }

                list.Add(result[i]);
                //result[i].TaggedAuth0Id = auth0IdDict[result[i].TaggedId];
            }

            return output;
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("balances")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> GetBalances([FromBody] BulkAuth0Request body)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            try
            {
                string[] authIds = body.Ids;//(string[])JObject.Parse(req)["ids"];

                var response = await _dbContext.UserWallet.Join(_dbContext.UserIdentity, (wallet) => wallet.UserId, (user) => user.UserId, (wallet, user) =>
                new
                {
                    user,
                    wallet
                }).Where(x => authIds.Contains(x.user.Auth0Id)).Select(x => new
                {
                    balance = x.wallet.Balance,
                    id = x.user.Auth0Id
                }).ToArrayAsync();

                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/getmfastate")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> GetMFAState([FromRoute] string id, [FromBody] SetMFAState body)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            var platformId = BitcornUtils.GetPlatformId(id);
            var fullUser = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (fullUser != null)
            {
                return new
                {
                    Mfa = fullUser.MFA
                };
            }
            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}/setmfastate")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> SetMFAState([FromRoute] string id, [FromBody] SetMFAState body)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            var platformId = BitcornUtils.GetPlatformId(id);
            var fullUser = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (fullUser != null)
            {
                if (body.Enabled != fullUser.MFA)
                {
                    var user = await _dbContext.User.FirstOrDefaultAsync(x => x.UserId == fullUser.UserId);
                    user.MFA = body.Enabled;
                    await _dbContext.SaveAsync();
                }
                return StatusCode(200);
            }
            else
            {
                return StatusCode(404);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/mission")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<UserMissionResponse>> GetUserMission([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var userMission = await _dbContext.UserMission.FirstOrDefaultAsync(x => x.UserId == user.UserId);
                var faucetClaimLeaderboard = await _dbContext.UserMission.OrderByDescending(x => x.FaucetClaimCount).Select(x => x.UserId).ToArrayAsync();
                int rank = Array.IndexOf(faucetClaimLeaderboard, userMission.UserId);
                if (userMission == null)
                {
                    return new UserMissionResponse(null, -1);
                }
                else
                {
                    return new UserMissionResponse(userMission, rank);
                }
            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/mission/{missionId}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<UserMissionResponse>> TryCompleteMission([FromRoute] string id, [FromRoute] string missionId)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var userMission = await _dbContext.UserMission.FirstOrDefaultAsync(x => x.UserId == user.UserId);
                bool changes = false;
                if (userMission == null)
                {
                    userMission = new UserMission();
                    userMission.UserId = user.UserId;
                    _dbContext.UserMission.Add(userMission);
                    changes = true;
                }

                if (missionId == "faucet")
                {
                    DateTime? previousClaimTime = userMission.Faucet;
                    var now = DateTime.Now;
                    var shouldClaim = false;
                    var sub = await SubscriptionUtils.GetActiveSubscription(_dbContext, user, "BITCORNFarms", 1).FirstOrDefaultAsync();//SubscriptionUtils.HasSubscribed(_dbContext, user, "BITCORNFarms", 1);
                    if (sub != null)
                    {
                        int prevSubCount = sub.UserSubcriptionTierInfo.UserSubscription.SubCount;

                        if (userMission.Faucet == null)
                        {
                            userMission.Faucet = now;
                            userMission.FaucetClaimCount = 1;
                            userMission.FaucetFarmAmount = 0;
                            userMission.FaucetClaimsDuringSub = 1;
                            userMission.FaucetClaimStreak = 1;
                            userMission.FaucetSubIndex = prevSubCount;
                            changes = true;
                            shouldClaim = true;
                        }
                        else
                        {
                            if (now > userMission.Faucet.Value.AddHours(24))
                            {
                                changes = true;
                                shouldClaim = true;

                                userMission.FaucetClaimCount++;
                                if (userMission.FaucetSubIndex != prevSubCount)
                                {
                                    userMission.FaucetSubIndex = prevSubCount;
                                    userMission.FaucetClaimsDuringSub = 1;
                                }
                                else
                                {
                                    userMission.FaucetClaimsDuringSub++;
                                }

                                if (now > userMission.Faucet.Value.AddHours(48))
                                {
                                    userMission.FaucetClaimStreak = 1;
                                }
                                else
                                {
                                    if (userMission.FaucetClaimStreak == null) userMission.FaucetClaimStreak = 1;
                                    userMission.FaucetClaimStreak++;
                                }

                                userMission.Faucet = now;
                            }
                        }
                    }
                    else
                    {

                    }

                    if (changes)
                    {
                        await _dbContext.SaveAsync();
                        if (shouldClaim)
                        {

                            var srcMission = await _dbContext.UserMission.FirstOrDefaultAsync(x => x.UserId == user.UserId);
                            if (srcMission.Faucet == now)
                            {
                                int replicate = 1;
                                if (previousClaimTime != null && (now - previousClaimTime.Value).TotalDays > 15)
                                {
                                    userMission.FaucetClaimCount += 14;
                                    replicate = 15;
                                }

                                var reward = 4166.666m; ;
                                var amount = reward * replicate;

                                decimal bonus = 0;
                                if (userMission.FaucetClaimStreak > 1)
                                {
                                    bonus = amount * 0.25m;
                                }

                                amount += bonus;
                                var tx = await TxUtils.SendFromBitcornhub(user, amount, "BITCORNFarms", "faucet", _dbContext);
                                if (tx)
                                {
                                    if (srcMission.FaucetFarmAmount == null)
                                    {
                                        srcMission.FaucetFarmAmount = 0;
                                    }
                                    if (srcMission.FaucetStreakBonuses == null)
                                    {
                                        srcMission.FaucetStreakBonuses = 0;
                                    }
                                    if (bonus > 0)
                                    {
                                        srcMission.FaucetStreakBonuses += bonus;
                                    }
                                    srcMission.FaucetFarmAmount += amount;
                                    await _dbContext.SaveAsync();
                                    userMission = srcMission;
                                }
                            }
                        }
                    }
                }

                var faucetClaimLeaderboard = await _dbContext.UserMission.OrderByDescending(x => x.FaucetClaimCount).Select(x => x.UserId).ToArrayAsync();
                int rank = Array.IndexOf(faucetClaimLeaderboard, userMission.UserId);
                return new UserMissionResponse(userMission, rank);

            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/social/comment/{commentId}/delete")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<SocialCommentResponse>> DeleteComment([FromRoute] string id, [FromRoute] string commentId, [FromQuery] string reader)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var comment = await _dbContext.SocialComment.FirstOrDefaultAsync(x => x.CommentId == commentId);
                if (comment != null)
                {
                    if (comment.UserId == user.UserId)
                    {
                        if (comment.IsListed)
                        {
                            comment.IsListed = false;
                            await _dbContext.SaveAsync();
                        }
                    }
                }
                var tags = new JoinedSocialTag[0];
                if (comment.IsListed)
                {
                    var queryResult = await CommentUtils.JoinTags(_dbContext).Where(c => c.Tag.CommentId == comment.CommentId).ToArrayAsync();
                    tags = queryResult.Select(x => new JoinedSocialTag(x.Tag, x.Identity)).ToArray();
                }

                return new SocialCommentResponse(comment, user.UserIdentity, tags);
            }

            return StatusCode(404);
        }

        public class ReadNotificationsBody
        {
            public string CommentId { get; set; }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}/social/notifications")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> ReadNotifications([FromRoute] string id, [FromBody] ReadNotificationsBody body)
        {

            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                SocialTag[] tags = null;
                if (body.CommentId == null)
                {
                    tags = await _dbContext.SocialTag.Where((s) => s.TagUserId == user.UserId && !s.Seen).ToArrayAsync();

                }
                else
                {
                    tags = await _dbContext.SocialTag.Where((s) => s.TagUserId == user.UserId && !s.Seen && s.CommentId == body.CommentId).ToArrayAsync();

                }

                for (int i = 0; i < tags.Length; i++)
                {
                    tags[i].Seen = true;
                }

                if (tags.Length > 0)
                {
                    await _dbContext.SaveAsync();
                }
                return await CommentUtils.GetNotifications(_dbContext, user);
            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/social/notifications")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> GetNotifications([FromRoute] string id)
        {

            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                return await CommentUtils.GetNotifications(_dbContext, user);

                /*
                var tags = await _dbContext.SocialTag
                    .Join(_dbContext.SocialComment)
                    .Where(x=>x.TagUserId==user.UserId&&!x.Seen)
                    
                    .ToArrayAsync();
                */
            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}/setsocketconnected/{state}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> SetSocketConnected([FromRoute] string id, [FromRoute] bool state)
        {
            try
            {
                if (this.GetCachedUser() != null)
                    throw new InvalidOperationException();
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

                var platformId = BitcornUtils.GetPlatformId(id);
                var fullUser = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (fullUser != null && !fullUser.IsBanned)
                {
                    if (fullUser.IsSocketConnected != state)
                    {
                        var user = await _dbContext.User.FirstOrDefaultAsync(x => x.UserId == fullUser.UserId);

                        user.IsSocketConnected = state;
                        int rows = await _dbContext.SaveAsync();
                    }

                    return new
                    {
                        isConnected = fullUser.IsSocketConnected
                    };
                }

                return StatusCode(404);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/social/follows/{followId}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> IsFollowing([FromRoute] string id, [FromRoute] string followId)
        {

            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            var followPlatformId = BitcornUtils.GetPlatformId(followId);
            var followUser = await BitcornUtils.GetUserForPlatform(followPlatformId, _dbContext).FirstOrDefaultAsync();

            if (user != null && !user.IsBanned)
            {
                var isFollowing = await _dbContext.SocialFollow.AnyAsync((f) => f.UserId == user.UserId && f.FollowId == followUser.UserId);
                return new
                {
                    IsFollowing = isFollowing
                };

            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/social/follow/{followId}/{doAction}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> Follow([FromRoute] string id, [FromRoute] string followId, [FromRoute] string doAction)
        {

            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            var followPlatformId = BitcornUtils.GetPlatformId(followId);
            var followUser = await BitcornUtils.GetUserForPlatform(followPlatformId, _dbContext).FirstOrDefaultAsync();

            if (user != null && followUser != null && !user.IsBanned)
            {
                var follow = await _dbContext.SocialFollow.FirstOrDefaultAsync((f) => f.UserId == user.UserId && f.FollowId == followUser.UserId);

                if (doAction == "unfollow")
                {
                    if (follow != null)
                    {
                        _dbContext.Remove(follow);
                        await _dbContext.SaveAsync();
                    }

                    return new
                    {
                        IsFollowing = false
                    };
                }
                else if (doAction == "follow")
                {
                    if (follow == null)
                    {
                        follow = new SocialFollow();
                        follow.FollowId = followUser.UserId;
                        follow.UserId = user.UserId;
                        _dbContext.SocialFollow.Add(follow);
                        await _dbContext.SaveAsync();
                    }

                    return new
                    {
                        IsFollowing = true
                    };

                    //
                }


            }

            return StatusCode(404);

        }


        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/social/comment")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<SocialCommentResponse>> CreateComment([FromRoute] string id, [FromBody] PostCommentBody body)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            try
            {
                if (body.Message.Length > 250) return StatusCode(404);

                var platformId = BitcornUtils.GetPlatformId(id);
                var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null && !user.IsBanned)
                {
                    if (string.IsNullOrEmpty(body.ParentId))
                        body.ParentId = null;
                    if (string.IsNullOrEmpty(body.MediaId))
                        body.MediaId = null;

                    string parentCommentId = null;
                    string rootCommentId = null;
                    string commentContext = null;
                    string commentContextId = null;
                    //bool isContextComment = false;
                    if (!string.IsNullOrEmpty(body.ParentId))
                    {
                        var parentComment = await _dbContext.SocialComment.FirstOrDefaultAsync(x => x.CommentId == CommentUtils.GetId(body.ParentId));
                        if (parentComment != null)
                        {
                            commentContext = parentComment.Context;
                            commentContextId = parentComment.ContextId;
                            parentCommentId = parentComment.CommentId;
                            var rootComment = await CommentUtils.GetRootComment(_dbContext, parentComment);
                            if (rootComment != null)
                            {
                                rootCommentId = rootComment.CommentId;
                            }
                        }

                    }
                    else
                    {
                        commentContext = body.Context;
                        commentContextId = body.ContextId;
                    }
                    /*
                    var timeCheck = DateTime.Now.AddSeconds(30);

                    var inCooldown = await _dbContext.SocialComment
                        .Where(s=>s.UserId==user.UserId && s.Timestamp<timeCheck).AnyAsync();

                    if (inCooldown) return StatusCode(420);
                    */
                    var removeTagsSentFromClient = new Regex(string.Format("\\{0}.*?\\{1}", "<tag:", ">"));

                    var rawMessage = removeTagsSentFromClient.Replace(body.Message, string.Empty);
                    //CommentUtils.GetId(body.ParentId)
                    var comment = CommentUtils.CreateComment(user.UserId, rawMessage, rootCommentId, parentCommentId);
                    comment.Context = commentContext;
                    comment.ContextId = commentContextId;
                    /*
                    if (!string.IsNullOrEmpty(body.ParentId) && isContextComment)
                    {
                        comment.Context = CommentUtils.GetContext(body.ParentId);
                        //comment.CommentId = CommentUtils.GetId(body.Context);

                    }
                    */
                    comment.MediaId = body.MediaId;

                    var messageBuilder = new StringBuilder(rawMessage.ToLower());
                    var splitMsg = body.Message.ToLower().Split(" ");

                    var tags = splitMsg.Where(x => x.Length > 0 && x[0] == '@').Select(x => x.Remove(0, 1)).ToArray();
                    var srcUsers = await CommentUtils.FindUsersByName(_dbContext, tags).Select((u) => new
                    {
                        UserId = u.UserId,
                        NickName = u.Auth0Nickname,
                        Auth0Id = u.Auth0Id,
                        Identity = u
                    }).ToDictionaryAsync(u => u.NickName.ToLower(), u => u);

                    List<JoinedSocialTag> newTags = new List<JoinedSocialTag>();
                    int tagIndex = 0;
                    for (int i = 0; i < tags.Length; i++)
                    {
                        var srcTag = tags[i];
                        if (srcUsers.TryGetValue(srcTag.ToLower(), out var obj))
                        {
                            var username = $"@{obj.NickName.ToLower()}";

                            if (!messageBuilder.ToString().Contains(username)) continue;

                            var tag = new SocialTag();
                            tag.CommentId = comment.CommentId;
                            tag.TagUserId = obj.UserId;
                            tag.Seen = false;
                            tag.Idx = tagIndex;

                            var insertString = $"<tag:{tagIndex}>";
                            messageBuilder.Replace(username, insertString);
                            tagIndex++;
                            newTags.Add(new JoinedSocialTag(tag, obj.Identity));
                            _dbContext.SocialTag.Add(tag);
                        }
                    }


                    comment.Message = messageBuilder.ToString();
                    _dbContext.SocialComment.Add(comment);
                    await _dbContext.SaveAsync();

                    return new SocialCommentResponse(comment, user.UserIdentity, newTags);
                }

                return StatusCode(404);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/social/comment/{commentId}/interact/{type}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<SocialCommentResponse>> CommentInteraction([FromRoute] string id, [FromRoute] string commentId, [FromRoute] string type, [FromQuery] string reader)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                commentId = CommentUtils.GetId(commentId);
                var comment = await _dbContext.SocialComment.Where(c => c.CommentId == commentId).FirstOrDefaultAsync();

                if (comment != null)
                {
                    if (comment.UserId == TxUtils.BitcornHubPK) return StatusCode(404);

                    int? previousInteractionType = null;
                    SocialCommentInteraction previousInteraction = null;
                    SocialCommentInteraction interaction = null;
                    if (type != "tip")
                    {
                        previousInteraction =
                            await _dbContext.SocialCommentInteraction
                            .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId
                                && c.UserId == user.UserId
                                && c.Type != 2);
                    }

                    if (previousInteraction != null)
                    {
                        interaction = previousInteraction;
                        previousInteractionType = interaction.Type;
                    }
                    else
                    {
                        if (type != "undo")
                        {
                            interaction = new SocialCommentInteraction();
                            interaction.UserId = user.UserId;
                            interaction.CommentId = comment.CommentId;
                            _dbContext.SocialCommentInteraction.Add(interaction);
                            //await _dbContext.SaveAsync();
                        }
                    }


                    if (type == "like")
                    {
                        interaction.Type = 1;
                        if (previousInteractionType == -1)
                        {
                            comment.Dislikes--;
                        }

                        if (previousInteractionType != 1)
                        {
                            comment.Likes++;
                        }

                    }

                    if (type == "dislike")
                    {
                        interaction.Type = -1;
                        if (previousInteractionType == 1)
                        {
                            comment.Likes--;

                        }

                        if (previousInteractionType != -1)
                        {
                            comment.Dislikes++;
                        }
                    }


                    if (type == "tip")
                    {
                        interaction.Type = 2;
                        comment.TipCount++;
                    }

                    if (type == "undo")
                    {

                        if (previousInteractionType == 1)
                        {
                            comment.Likes--;
                        }

                        if (previousInteractionType == -1)
                        {
                            comment.Dislikes--;
                        }

                        interaction.Type = -10;

                    }

                    await _dbContext.SaveAsync();
                    var tags = await CommentUtils.JoinTags(_dbContext)
                        .Where(c => c.Tag.CommentId == comment.CommentId).ToArrayAsync();
                    return new SocialCommentResponse(comment,
                        await _dbContext.UserIdentity.FirstOrDefaultAsync(u => u.UserId == comment.UserId),
                        tags.Select(x => new JoinedSocialTag(x.Tag, x.Identity)));
                }
            }

            return StatusCode(404);
        }





        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/social/comments")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetComments([FromRoute] string id, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var rootComments = await _dbContext.SocialComment.Where(c => c.UserId == user.UserId && c.ParentId == null && c.IsListed)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(10)
                    .ToArrayAsync();
                var tagDict = await CommentUtils.GetAllTags(_dbContext, rootComments);
                return rootComments.Select(x => new SocialCommentResponse(x, user.UserIdentity, tagDict[x.CommentId])).ToArray();
                /*
                var rootChecks = rootComments.Select(e=>e.CommentId).ToHashSet();
                var subComments = await _dbContext.SocialComment.Where(c=>rootChecks.Contains(c.ParentId)).ToArrayAsync();
                */
            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("social/commentbyid/{commentId}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetCommentById([FromRoute] string commentId)
        {

            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            commentId = CommentUtils.GetId(commentId);

            var comments = await CommentUtils.JoinUser(_dbContext).Where((x) =>
                x.comment.CommentId == commentId && x.comment.IsListed
            ).ToArrayAsync();

            var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());
            return comments.Select((c) => new SocialCommentResponse(c.comment, c.identity, tagDict[c.comment.CommentId])).ToArray();



            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("social/comments/context/{contextId}")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetCommentsByContext([FromRoute] string contextId, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();


            var comments = await CommentUtils.JoinUser(_dbContext)
                .Where(c => c.comment.ContextId == contextId && c.comment.ParentId == null && c.comment.IsListed)
                  .OrderByDescending(x => x.comment.Timestamp)
                  .Take(10)
                  .ToArrayAsync();
            //var tagDict = await CommentUtils.GetAllTags(_dbContext, rootComments);
            var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());
            return comments.Select((c) => new SocialCommentResponse(c.comment, c.identity, tagDict[c.comment.CommentId])).ToArray();

            //return rootComments.Select(x => new SocialCommentResponse(x, user.UserIdentity, tagDict[x.CommentId])).ToArray();

        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/social/feed")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetFeed([FromRoute] string id)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null && !user.IsBanned)
                {
                    var following = await _dbContext.SocialFollow.Where(u => u.UserId == user.UserId).ToArrayAsync();
                    var followingUserIds = following.Select((f) => f.FollowId).ToArray();

                    var comments = await CommentUtils.JoinUser(_dbContext)
                        .Where((x) => x.comment.IsListed && followingUserIds.Contains(x.comment.UserId) && x.comment.ParentId == null && !x.user.IsBanned && x.comment.Context == null)

                        .OrderByDescending(x => x.comment.Timestamp)
                            .Take(10)
                            .ToArrayAsync();

                    var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());
                    return comments.Select((c) => new SocialCommentResponse(c.comment, c.identity, tagDict[c.comment.CommentId])).ToArray();

                }

                return StatusCode(404);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("social/comment/{commentId}/subcomments")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetSubComments([FromRoute] string commentId, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            commentId = CommentUtils.GetId(commentId);
            var comment = await _dbContext.SocialComment.FirstOrDefaultAsync(c => c.CommentId == commentId);
            if (comment != null)
            {
                var comments = await CommentUtils.JoinUser(_dbContext)
                    .Where((c) => c.comment.ParentId == comment.CommentId && !c.user.IsBanned)

                    .OrderByDescending(x => x.comment.Timestamp)
                    .Take(10)
                    .ToArrayAsync();

                var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());

                return comments.Select(x => new SocialCommentResponse(x.comment, x.identity, tagDict[x.comment.CommentId])).ToArray();
            }



            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/referralinfo")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> GetReferralInfo([FromRoute] string id, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                var referrer = await _dbContext.Referrer.FirstOrDefaultAsync(r => r.UserId == user.UserId);
                var userReferral = await _dbContext.UserReferral.FirstOrDefaultAsync(r => r.UserId == user.UserId);
                var userStats = await _dbContext.UserStat.FirstOrDefaultAsync(r => r.UserId == user.UserId);
                Dictionary<string, object> output = new Dictionary<string, object>();
                if (referrer != null)
                {
                    BitcornUtils.AppendUserOutput(output, new Type[] { typeof(decimal?), typeof(int?), typeof(string), typeof(DateTime?), typeof(int) }, referrer, "userId");

                    /*fullUser.ReferralId = referrer.ReferralId;
                    fullUser.Amount = referrer.Amount;
                    fullUser.Tier = referrer.Tier;
                    fullUser.ETag = referrer.ETag;
                    fullUser.Key = referrer.Key;
                    fullUser.YtdTotal = referrer.YtdTotal;*/
                }

                if (userReferral != null)
                {
                    BitcornUtils.AppendUserOutput(output, new Type[] { typeof(decimal), typeof(int), typeof(string), typeof(DateTime?), typeof(int?) }, userReferral, "userId", "referralId");
                    /*
                    fullUser.WalletDownloadDate = userReferral.WalletDownloadDate;
                    fullUser.MinimumBalanceDate = userReferral.MinimumBalanceDate;
                    fullUser.SyncDate = userReferral.SyncDate;
                    fullUser.SignupReward = userReferral.SignupReward;
                    fullUser.Bonus = userReferral.Bonus;
                    fullUser.ReferrerBonus = userReferral.ReferrerBonus;
                    */
                }
                if (userStats != null)
                {
                    output.Add("totalReferrals", userStats.TotalReferrals);
                    output.Add("totalReferralRewardsCorn", userStats.TotalReferralRewardsCorn);
                    output.Add("totalReferralRewardsUsdt", userStats.TotalReferralRewardsUsdt);
                }
                return output;
                //
                //return BitcornUtils.GetFullUserAndReferer(user, user.UserIdentity, user.UserWallet, user.UserStat, user.UserReferral, referral);
            }
            else
            {
                return StatusCode(404);
            }
        }
        /* */
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}/[action]")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> SelectProperties([FromRoute] string id, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                var referral = _dbContext.Referrer.FirstOrDefault(r => r.UserId == user.UserId);
                bool isGuest = id != reader;
                return BitcornUtils.SelectUserProperties(user, user.UserIdentity, user.UserWallet, user.UserStat, user.UserReferral, referral, isGuest);
            }
            else
            {
                return StatusCode(404);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("me")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public ActionResult<FullUser> Me()
        {
            User user = null;
            if ((user = this.GetCachedUser()) == null)
                return StatusCode(404);
            return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
        }
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("unlockwallet")]
        [Authorize(Policy = AuthScopes.BanUser)]
        public async Task<ActionResult<object>> UnlockWallet([FromBody] UnlockUserWalletRequest request)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(request.Sender)) throw new ArgumentNullException("Sender");

            if (string.IsNullOrWhiteSpace(request.UnlockUser)) throw new ArgumentNullException("UnlockUser");
            var senderPlatformId = BitcornUtils.GetPlatformId(request.Sender);
            var senderUser = await BitcornUtils.GetUserForPlatform(senderPlatformId, _dbContext).FirstOrDefaultAsync();
            if (senderUser != null && senderUser.IsAdmin())
            {
                var userPlatformId = BitcornUtils.GetPlatformId(request.UnlockUser);


                var unlockUser = await BitcornUtils.GetUserForPlatform(userPlatformId, _dbContext).FirstOrDefaultAsync();
                if (unlockUser != null && unlockUser.UserWallet.IsLocked != null && unlockUser.UserWallet.IsLocked == true)
                {
                    unlockUser.UserWallet.IsLocked = false;

                    await _dbContext.SaveAsync();
                }

                return new
                {
                    IsLocked = unlockUser.UserWallet.IsLocked
                };
            }
            else
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("ban")]
        [Authorize(Policy = AuthScopes.BanUser)]
        public async Task<ActionResult<object>> Ban([FromBody] BanUserRequest request)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(request.Sender)) throw new ArgumentNullException("Sender");

            if (string.IsNullOrWhiteSpace(request.BanUser)) throw new ArgumentNullException("BanUser");

            var senderPlatformId = BitcornUtils.GetPlatformId(request.Sender);
            var senderUser = await BitcornUtils.GetUserForPlatform(senderPlatformId, _dbContext).FirstOrDefaultAsync();
            if (senderUser != null && senderUser.IsAdmin())
            {
                var banPlatformId = BitcornUtils.GetPlatformId(request.BanUser);
                var primaryKey = -1;

                var banUser = await BitcornUtils.GetUserForPlatform(banPlatformId, _dbContext).FirstOrDefaultAsync();
                if (banUser != null)
                {
                    primaryKey = banUser.UserId;
                    banUser.IsBanned = true;
                    _dbContext.Update(banUser);

                    await _dbContext.SaveAsync();
                }
                var users = await UserReflection.GetColumns(_dbContext, new string[] { "*" }, new[] { primaryKey });
                if (users.Count > 0)
                    return users.First();
                return null;
            }
            else
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }
        }
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{name}/[action]")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<bool> Check(string name)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            return await _dbContext.User.AnyAsync(u => u.Username == name);
        }

        [Authorize(Policy = AuthScopes.ChangeUser)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPut("[action]")]
        public async Task<bool> Update([FromBody] Auth0IdUsername auth0IdUsername)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (_dbContext.User.Any(u => u.Username == auth0IdUsername.Username))
            {
                return false;
            }
            //join identity with user table to select in 1 query
            var user = await _dbContext.Auth0Query(auth0IdUsername.Auth0Id)
                .Join(_dbContext.User, identity => identity.UserId, us => us.UserId, (id, u) => u).FirstOrDefaultAsync();

            user.Username = auth0IdUsername.Username;
            await _dbContext.SaveAsync();
            return true;
        }
    }
}