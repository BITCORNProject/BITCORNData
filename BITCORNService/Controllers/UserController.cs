using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
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
                    auth0Nickname = "",//user.UserIdentity.Auth0Nickname + str
                    username = user.UserIdentity.Username
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

        public class GetTransactionsv2Body
        {
            public string[] TxTypes { get; set; }
        }
        class TxTick
        {
            public DateTime Time { get; set; }
            public decimal Value { get; set; }
        }

        [Authorize(Policy = AuthScopes.ReadTransaction)]
        [HttpPost("transactions/v2/{id}/{offset}/{amount}")]
        public async Task<ActionResult<object>> TransactionsV2([FromRoute] string id, [FromRoute] int offset, [FromRoute] int amount, [FromBody] GetTransactionsv2Body body)
        {
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    string[] txTypesArr = body.TxTypes;//null;
                                                       //if (!string.IsNullOrEmpty(txTypes)) txTypesArr = txTypes.Split(" ");

                    var transactions = await Utils.Stats.CornTxUtils.ListTransactions(_dbContext, user.UserId, offset, amount, txTypesArr);
                    var startBalance = user.UserWallet.Balance;
                    //var sentAmount = transactions.Where(x => x.Action == "sent").Sum(x => x.Amount); ;
                    //var recAmount = transactions.Where(x => x.Action == "receive").Sum(x => x.Amount); ;

                    //startBalance -= sentAmount;

                    var ticks = new List<TxTick>();
                    if (transactions.Length > 0)
                    {
                        foreach (var tx in transactions)
                        {
                            if (tx.Action == "sent")
                            {
                                startBalance += tx.Amount;
                            }
                            else
                            {
                                startBalance -= tx.Amount;
                            }

                            ticks.Add(new TxTick
                            {
                                Time = tx.Time,
                                Value = startBalance.Value
                            });
                        }
                        /*
                        if (ticks.Count > 0)
                        {
                            var lastTime = ticks.Last().Time;
                            if(DateTime.Now<lastTime)
                            {
                                lastTime = lastTime.AddMinutes(10);
                            }
                            else
                            {
                                lastTime = DateTime.Now;
                            }
                            ticks.Add(new TxTick
                            {
                                Time = DateTime.Now,
                                Value = user.UserWallet.Balance.Value
                            });
                        }*/
                        //ticks = ticks.Take(amount / 2).ToList();

                        //ticks.Reverse();

                        /*
                        var firstTime = transactions.Last().Time;
                        ticks.Add(new { 
                            time = firstTime.AddDays(-1),
                            value = startBalance
                        });
                        */

                    }
                    /*

                    */
                    return new
                    {
                        Transactions = transactions,
                        Ticks = ticks
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
                    NickName = u.Username,
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
                if (!string.IsNullOrEmpty(stream.User.UserIdentity.TwitchId))
                {
                    channels.Add(stream.User.UserIdentity.TwitchUsername);
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

            public string GiveawayText { get; set; }
            public DateTime? GiveawayEnd { get; set; }
            public bool GiveawayOpen { get; set; }
            public decimal GiveawayEntryFee { get; set; }
            public string TwitchUsername { get; set; }
            public bool hasTicket { get; set; }
            public PublicLivestreamsResponse()
            {

            }

            public PublicLivestreamsResponse(User user, UserLivestream stream, UserGiveawayTicket ticket)
            {

                AmountOfRainsSent = stream.AmountOfRainsSent;
                AmountOfTipsSent = stream.AmountOfTipsSent;
                TwitchId = user.UserIdentity.TwitchId;
                TwitchUsername = user.UserIdentity.TwitchUsername;
                TotalSentBitcornViaRains = stream.TotalSentBitcornViaRains;
                TotalSentBitcornViaTips = stream.TotalSentBitcornViaTips;
                IrcPayments = stream.IrcEventPayments;
                Tier3IdlePerMinute = stream.Tier3IdlePerMinute;
                Tier1IdlePerMinute = stream.Tier1IdlePerMinute;
                Tier2IdlePerMinute = stream.Tier2IdlePerMinute;
                IsPartner = stream.BitcornhubFunded;
                Auth0Id = user.UserIdentity.Auth0Id;
                GiveawayEnd = stream.GiveawayEnd;
                GiveawayEntryFee = stream.GiveawayEntryFee;
                GiveawayOpen = stream.GiveawayOpen;
                GiveawayText = stream.GiveawayText;
                if (ticket != null)
                {
                    hasTicket = true;
                }
            }
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
                if (!string.IsNullOrEmpty(entry.User.UserIdentity.TwitchId))
                {
                    channels.Add(new PublicLivestreamsResponse(entry.User, entry.Stream, null));
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
                public string ChannelPointCardId { get; set; }
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

            var userIds = request.Where(x => !string.IsNullOrEmpty(x.RefreshToken)).Select(x => x.IrcTarget).ToArray();

            var userIdentities = await _dbContext.UserIdentity.Join(_dbContext.UserLivestream, (u) => u.UserId, (s) => s.UserId, (u, s) => new
            {
                identity = u,
                stream = s
            }).Where(u => userIds.Contains(u.identity.TwitchId)).ToDictionaryAsync(x => x.identity.TwitchId, x => x);
            int changeCount = 0;
            for (int i = 0; i < request.Length; i++)
            {
                var req = request[i];
                if (userIdentities.TryGetValue(req.IrcTarget, out var entry))
                {
                    if (!string.IsNullOrEmpty(entry.identity.TwitchRefreshToken))
                    {
                        entry.identity.TwitchRefreshToken = req.RefreshToken;
                        entry.stream.ChannelPointCardId = req.ChannelPointCardId;

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


        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("livestreams/settings")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object[]>> GetLivestreamSettings([FromQuery] string columns)
        {
            string[] selectColumns = new string[0];
            if (!string.IsNullOrEmpty(columns))
            {
                selectColumns = columns.Split(" ");
            }
            return await LivestreamUtils.GetLivestreamSettings(_dbContext, selectColumns);
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
        public async Task<ActionResult<object>> GetPublicLivestream([FromRoute] string id, [FromQuery] string reader)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var liveStream = await _dbContext.UserLivestream.Where(x => x.UserId == user.UserId).FirstOrDefaultAsync();
                if (liveStream != null && liveStream.Public && !string.IsNullOrEmpty(user.UserIdentity.TwitchId))
                {
                    UserGiveawayTicket readerTicket = null;
                    /*
                    if (!string.IsNullOrEmpty(reader))
                    {
                        var readerPlatformId = BitcornUtils.GetPlatformId(reader);
                        var readerUser = await BitcornUtils.GetUserForPlatform(readerPlatformId, _dbContext).FirstOrDefaultAsync();
                        if (readerUser != null)
                        {
                            //var ticket = await _dbContext.UserGiveawayTicket.Where(x => x.UserId == readerUser.UserId && user.UserId == x.ChannelId).FirstOrDefaultAsync();
                            if (ticket != null && ticket.GiveawayIndex == liveStream.GiveawayIndex)
                            {
                                readerTicket = ticket;
                            }
                        }
                    }*/
                    return new PublicLivestreamsResponse(user, liveStream, readerTicket);
                }
            }

            return new PublicLivestreamsResponse();
            //return StatusCode(404);
        }


        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/livestream")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<Object>> GetLivestream([FromRoute] string id)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    var stream = await _dbContext.GetLivestreams(false).Where(e => e.User.UserId == user.UserId).FirstOrDefaultAsync();
                    if (stream != null)
                    {
                        /*
                        var properties = typeof(UserLivestream).GetProperties(BindingFlags.GetProperty|BindingFlags.Public);
                        Dictionary<string, object> output = new Dictionary<string, object>();
                        foreach (var property in properties)
                        {
                            var name = property.Name;
                            output.Add(Char.ToLowerInvariant(name[0]) + name.Substring(1), property.GetValue(stream.Stream));
                        }
                     
                        return output;
                        //
                        */
                        return stream.Stream;
                    }
                }

                return StatusCode(404);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpGet("accesslocked")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public ActionResult CheckAccessLock()
        {
            return StatusCode(200);
        }
        /*
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/livestream/actions")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> GetStreamActions([FromRoute] string id)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                var actions = await _dbContext.UserStreamAction.Where(x => x.RecipientUserId == user.UserId).ToArrayAsync();
                for (int i = 0; i < actions.Length; i++)
                {
                    actions[i].Closed = true;
                }

                if (actions.Length > 0)
                {
                    await _dbContext.SaveAsync();
                }

                return actions;
            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/livestream/tts")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> GetTts([FromRoute] string id)
        {
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    var userTts = await _dbContext.UserTts.FirstOrDefaultAsync(x => x.UserId == user.UserId);
                    if (userTts == null)
                    {
                        userTts = new UserTts();
                        userTts.Rate = 1;
                        userTts.Pitch = 1;
                        userTts.Voice = 0;
                        userTts.UserId = user.UserId;
                        _dbContext.UserTts.Add(userTts);
                        await _dbContext.SaveAsync();
                    }

                    return userTts;
                }

                return StatusCode(404);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}/livestream/tts")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> SetTts([FromRoute] string id, [FromBody] UserTts body)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    var userTts = await _dbContext.UserTts.FirstOrDefaultAsync(x => x.UserId == user.UserId);
                    if (userTts == null)
                    {
                        userTts = new UserTts();
                        userTts.UserId = user.UserId;
                        _dbContext.UserTts.Add(userTts);
                    }

                    userTts.Voice = body.Voice;
                    userTts.Rate = body.Rate;
                    userTts.Pitch = body.Pitch;

                    await _dbContext.SaveAsync();
                    return userTts;
                }

                return StatusCode(404);
            }
            catch (Exception ex)
            {
                throw ex;
                //return StatusCode(404);

            }
        }

        */
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
                            Enabled = true,//body.Enabled,
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
                            Tier1SubReward = 420,
                            Tier2SubReward = 4200,
                            Tier3SubReward = 42000,
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
                        bool commit = true;
                        if (liveStream.LastUpdateTime == null)
                        {
                            liveStream.LastUpdateTime = DateTime.Now;
                        }
                        else
                        {
                            if (DateTime.Now < liveStream.LastUpdateTime.Value.AddMilliseconds(100))
                            {
                                commit = false;
                            }
                            else
                            {
                                liveStream.LastUpdateTime = DateTime.Now;
                            }
                        }

                        if (commit)
                        {
                            bool changes = false;
                            /*if (body.Enabled != liveStream.Enabled)
                            {
                                changes = true;
                                liveStream.Enabled = body.Enabled;
                            }*/

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

                            bool allowFundedChanges = user.IsAdmin();//true;
                            if (!liveStream.BitcornhubFunded || allowFundedChanges)
                            {
                                if (body.Tier1IdlePerMinute != liveStream.Tier1IdlePerMinute)
                                {
                                    liveStream.Tier1IdlePerMinute = body.Tier1IdlePerMinute;
                                    if (liveStream.Tier1IdlePerMinute <= 0)
                                        liveStream.Tier1IdlePerMinute = 0;

                                    if (liveStream.Tier1IdlePerMinute > 1000) liveStream.Tier1IdlePerMinute = 1000;

                                    changes = true;
                                }

                                if (body.Tier2IdlePerMinute != liveStream.Tier2IdlePerMinute)
                                {
                                    liveStream.Tier2IdlePerMinute = body.Tier2IdlePerMinute;
                                    if (liveStream.Tier2IdlePerMinute <= 0)
                                        liveStream.Tier2IdlePerMinute = 0;

                                    if (liveStream.Tier2IdlePerMinute > 1000) liveStream.Tier2IdlePerMinute = 1000;
                                    changes = true;
                                }

                                if (body.Tier3IdlePerMinute != liveStream.Tier3IdlePerMinute)
                                {
                                    liveStream.Tier3IdlePerMinute = body.Tier3IdlePerMinute;
                                    if (liveStream.Tier3IdlePerMinute <= 0)
                                        liveStream.Tier3IdlePerMinute = 0;
                                    if (liveStream.Tier3IdlePerMinute > 1000) liveStream.Tier3IdlePerMinute = 1000;
                                    changes = true;
                                }

                                if (body.BitcornPerBit != liveStream.BitcornPerBit)
                                {
                                    liveStream.BitcornPerBit = body.BitcornPerBit;
                                    if (liveStream.BitcornPerBit <= 0)
                                        liveStream.BitcornPerBit = 0;
                                    changes = true;
                                }


                                if (body.Tier1SubReward != liveStream.Tier1SubReward)
                                {
                                    liveStream.Tier1SubReward = body.Tier1SubReward;
                                    if (liveStream.Tier1SubReward <= 0)
                                        liveStream.Tier1SubReward = 0;
                                    changes = true;
                                }

                                if (body.Tier2SubReward != liveStream.Tier2SubReward)
                                {
                                    liveStream.Tier2SubReward = body.Tier2SubReward;
                                    if (liveStream.Tier2SubReward <= 0)
                                        liveStream.Tier2SubReward = 0;
                                    changes = true;
                                }

                                if (body.Tier3SubReward != liveStream.Tier3SubReward)
                                {
                                    liveStream.Tier3SubReward = body.Tier3SubReward;
                                    if (liveStream.Tier3SubReward <= 0)
                                        liveStream.Tier3SubReward = 0;
                                    changes = true;
                                }

                                if (body.BitcornPerChannelpointsRedemption != liveStream.BitcornPerChannelpointsRedemption)
                                {
                                    liveStream.BitcornPerChannelpointsRedemption = body.BitcornPerChannelpointsRedemption;
                                    if (liveStream.BitcornPerChannelpointsRedemption <= 0m)
                                    {
                                        liveStream.BitcornPerChannelpointsRedemption = 0;
                                    }
                                    changes = true;
                                }


                                if (body.BitcornPerTtsCharacter != liveStream.BitcornPerTtsCharacter)
                                {
                                    liveStream.BitcornPerTtsCharacter = body.BitcornPerTtsCharacter;
                                    if (liveStream.BitcornPerTtsCharacter <= 0) liveStream.BitcornPerTtsCharacter = 0;
                                    changes = true;
                                }

                            }
                            if (body.EnableChannelpoints != liveStream.EnableChannelpoints)
                            {
                                liveStream.EnableChannelpoints = body.EnableChannelpoints;
                                changes = true;
                            }


                            /*
                            if (body.EnableTts != liveStream.EnableTts)
                            {
                                liveStream.EnableTts = body.EnableTts;
                                changes = true;
                            }
                            */
                            if (changes)
                            {


                                if (commit)
                                {
                                    var count = await _dbContext.SaveAsync();
                                    //WebSocketsController.GetSocketArgs<string[]>("update-livestream-settings");
                                    var columns = await UserReflection.GetColumns(_dbContext, WebSocketsController.BitcornhubSocketArgs, new int[] { user.UserId }, new UserReflectionContext(UserReflection.StreamerModel));
                                    Dictionary<string, object> selects = null;
                                    if (columns != null)
                                    {
                                        selects = columns.FirstOrDefault().Value;//.Add();
                                    }

                                    await WebSocketsController.TryBroadcastToBitcornhub(_dbContext, "update-livestream-settings",
                                        LivestreamUtils.GetLivestreamSettingsForUser(user, liveStream, selects));
                                }
                            }
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
                if (userMission == null)
                {
                    return new UserMissionResponse(null, -1, false);
                }

                if (userMission != null)
                {
                    var faucetClaimLeaderboard = await _dbContext.UserMission.OrderByDescending(x => x.FaucetClaimCount).Select(x => x.UserId).ToArrayAsync();
                    int rank = Array.IndexOf(faucetClaimLeaderboard, userMission.UserId);

                    {
                        return new UserMissionResponse(userMission, rank, false);
                    }
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
                bool wasCompleted = false;
                decimal completeReward = 0;
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
                                    completeReward = amount;
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
                                    wasCompleted = true;
                                }
                            }
                        }
                    }
                }

                var faucetClaimLeaderboard = await _dbContext.UserMission.OrderByDescending(x => x.FaucetClaimCount).Select(x => x.UserId).ToArrayAsync();
                int rank = Array.IndexOf(faucetClaimLeaderboard, userMission.UserId);
                return new UserMissionResponse(userMission, rank, wasCompleted, completeReward);

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
                if (referrer == null)
                {
                    referrer = new Referrer();
                    referrer.UserId = user.UserId;
                    referrer.Tier = 0;
                    referrer.YtdTotal = 0;
                    referrer.Amount = 10;
                    _dbContext.Referrer.Add(referrer);
                    await _dbContext.SaveAsync();

                }

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
                user.UserIdentity.TwitchRefreshToken = null;
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
            if (name.Length > 50) return false;
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            return await _dbContext.UserIdentity.AnyAsync(u => u.Username.ToLower() == name.ToLower());
        }

        [Authorize(Policy = AuthScopes.ChangeUser)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPut("[action]")]
        public async Task<bool> Update([FromBody] Auth0IdUsername auth0IdUsername)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (auth0IdUsername.Username.Length > 50)
            {
                return false;
            }
            if (_dbContext.UserIdentity.Any(u => u.Username.ToLower() == auth0IdUsername.Username.ToLower()))
            {
                return false;
            }
            /*
            //join identity with user table to select in 1 query
            var user = await _dbContext.Auth0Query(auth0IdUsername.Auth0Id)
                //.Join(_dbContext.User, identity => identity.UserId, us => us.UserId, (id, u) => u)
                .FirstOrDefaultAsync();
            */
            var user = await _dbContext.UserIdentity.Where(x=>x.Auth0Id==auth0IdUsername.Auth0Id).FirstOrDefaultAsync();
            if (user != null)
            {
                user.Username = auth0IdUsername.Username;
                
                int count = await _dbContext.SaveAsync();
                return true;
            }
            return false;
        }
    }
}