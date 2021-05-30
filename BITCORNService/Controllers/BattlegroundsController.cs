using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Games;
using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [Authorize(Policy = AuthScopes.ReadUser)]
    [Route("api/game/[controller]")]
    [ApiController]
    public class BattlegroundsController : ControllerBase
    {
        BitcornGameContext _dbContext = null;
        public BattlegroundsController(BitcornGameContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpGet("{host}/hosts")]
        public async Task<ActionResult<object>> Hosts([FromRoute] string host)
        {
            var hostIds = await _dbContext.BattlegroundsUser.Select(x => x.HostId).Distinct().ToArrayAsync();
            var identities = await _dbContext.UserIdentity.Where(x => hostIds.Contains(x.UserId)).Where(x=>!string.IsNullOrEmpty(x.TwitchId)).ToArrayAsync();
            var objs = identities.Select(x => new
            {
                twitchId = x.TwitchId,
                twitchUsername = x.TwitchUsername,
                userId = x.UserId
            }).ToArray();

            var platformId = BitcornUtils.GetPlatformId(host);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            string selectedHost = "";
            if (user != null)
            {
                selectedHost = objs.Where(x=>x.twitchId == user.UserIdentity.TwitchId).Select(x=>x.twitchId).FirstOrDefault();
              
            }

            if (selectedHost == null)
            {
                if (objs.Length > 0)
                {
                    selectedHost = objs[0].twitchId;
                }
            }


            return new
            {
                hosts = objs,
                selectedHost = "twitch|"+selectedHost
            };
        }

        [HttpGet("leaderboard/{host}/{orderby}")]
        public async Task<ActionResult<object>> Leaderboard([FromRoute] string host, [FromRoute] string orderby)
        {
            var properties = typeof(BattlegroundsUser)
                .GetProperties()
                .Select(p => p.Name.ToLower())
                .ToArray();

            int userId = -1;

            if (int.TryParse(host, out int Id))
            {
                userId = Id;
            }
            else
            {
                var platformId = BitcornUtils.GetPlatformId(host);
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null)
                {
                    userId = user.UserId;
                }
                else
                {
                    userId = 43;
                }
            }


            if (userId == -1) return StatusCode(404);

            {
                if (properties.Contains(orderby.ToLower()))
                {
                    return await _dbContext.BattlegroundsUser.Where(u => u.HostId == userId).OrderByDescending(orderby).Join(_dbContext.UserIdentity,
                        (stats) => stats.UserId,
                        (identity) => identity.UserId,
                        (s, i) => new
                        {
                            name = i.TwitchUsername,
                            stats = s
                        }).Join(_dbContext.User, (info) => info.stats.UserId, (user) => user.UserId, (selectedInfo, selectedUser) => new
                        {
                            name = selectedInfo.name,
                            stats = selectedInfo.stats,
                            isBanned = selectedUser.IsBanned

                        }).Where(u => !u.isBanned && (u.name != "" && u.name != null)).Take(100).ToArrayAsync();

                }
            }
            return StatusCode((int)HttpStatusCode.BadRequest);
        }

        [HttpGet("stats/{host}")]
        public async Task<ActionResult<object>> Stats([FromRoute] string host)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(host);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                var profile = await _dbContext.BattlegroundsUser.Where(x => x.UserId == user.UserId && x.HostId == user.UserId).FirstOrDefaultAsync();
                if (profile != null)
                {
                    var playedGames = await _dbContext.GameInstance.Where(x => x.HostId == user.UserId).CountAsync();
                    var uniquePlayers = await _dbContext.BattlegroundsUser.Where(x => x.HostId == user.UserId).CountAsync();
                    return new
                    {
                        bitcornEarned = profile.HostCornRewards,
                        playedGames = playedGames,
                        uniquePlayers = uniquePlayers
                    };
                }
            }

            return new
            {
                bitcornEarned = 0,
                playedGames = 0,
                uniquePlayers = 0
            };
        }


        [Authorize(Policy = AuthScopes.SendTransaction)]
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("bgrain")]
        public async Task<ActionResult<object>> BgRain([FromBody] RainRequest rainRequest)
        {
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value == 0)
            {
                throw new NotImplementedException();
            }
            
            if(!CheckPaidSupport())
            {
                return StatusCode(420);
            }

            var cachedUser = this.GetCachedUser();
            if (rainRequest == null) throw new ArgumentNullException();
            if (rainRequest.From == null) throw new ArgumentNullException();
            if (rainRequest.To == null) throw new ArgumentNullException();
            if (rainRequest.Amount <= 0) return StatusCode((int)HttpStatusCode.BadRequest);
            rainRequest.FromUser = this.GetCachedUser();
            User host = null;
            GameInstance activeGame = null;
            if (!string.IsNullOrEmpty(rainRequest.IrcTarget))
            {
                host = await _dbContext.TwitchQuery(rainRequest.IrcTarget).FirstOrDefaultAsync();
                activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == host.UserId && g.Active);

            }

            if (host != null && activeGame != null)
            {
                if (!activeGame.Bgrains) return StatusCode(420);

                var existingBgRainCount = await _dbContext.SignedTx.Where(x => x.GameInstanceId == activeGame.GameId).CountAsync();
                if (existingBgRainCount > 50) return StatusCode(420);
                var outputs = new List<(SignedTx, TxReceipt)>();
                foreach (var item in rainRequest.To)
                {
                    var signedTx = await TxUtils.CreateSignedTx(_dbContext, this.GetCachedUser(), rainRequest.Amount, "BITCORNBattlegrounds", "bgrain", activeGame.GameId);
                    if (signedTx != null)
                    {
                        outputs.Add(signedTx.Value);
                    }
                }

                if (outputs.Count > 0)
                {
                    var result = await WebSocketsController.TryBroadcastToBattlegroundsUser(host.UserIdentity.Auth0Id,
                                       _dbContext,
                                       "bgrain",
                                       new
                                       {
                                           sender = rainRequest.FromUser.UserIdentity.TwitchUsername,
                                           transactions = outputs.Select(x => new
                                           {
                                               signed = x.Item1,
                                               tx = x.Item2
                                           }).ToArray()
                                       }
                                       );

                    if (result == WebSocketsController.SocketBroadcastResult.Success)
                    {

                    }
                }

                var txOutputs = outputs.Select(x => x.Item2).ToArray();
                await TxUtils.AppendTxs(txOutputs, _dbContext, rainRequest.Columns);
                //UserReflection.GetColumns(_dbContext, rainRequest.Columns, );
                return txOutputs;
            }

            return StatusCode(404);
        }

        public class SwitchTeamRequest
        {
            public int UserId { get; set; }
            public int Team { get; set; }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("switchteam")]
        public async Task<ActionResult<object>> SwitchTeam([FromBody] SwitchTeamRequest request)
        {
            var sender = this.GetCachedUser();
            if (sender != null)
            {

                var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                if (activeGame != null)
                {
                    var profile = await _dbContext.BattlegroundsUser.Where(x => x.UserId == request.UserId && x.HostId == sender.UserId).FirstOrDefaultAsync();
                    profile.Team = request.Team;
                    await _dbContext.SaveAsync();
                    return new
                    {
                        Team = profile.Team.Value
                    };
                }
            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("join/v2")]
        public async Task<ActionResult<object>> JoinV2([FromBody] BattlegroundsJoinGameRequest2 request)
        {
            //if (this.GetCachedUser() != null)
            //throw new InvalidOperationException();
            bool supportPaidGames = false;
            User sender = null;
            var cachedUser = this.GetCachedUser();
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value == 0)
            {
                sender = cachedUser;
                supportPaidGames = sender.IsAdmin();
                //    sender = 
            }
            else if (userMode != null && userMode.Value == 1)
            {
                sender = await _dbContext.TwitchQuery(request.IrcTarget).FirstOrDefaultAsync();//this.GetCachedUser();
                supportPaidGames = true;

            }

            if (supportPaidGames) supportPaidGames = CheckPaidSupport();
            /*
            if (sender == null)
            {
                sender = await _dbContext.TwitchQuery(request.IrcTarget).FirstOrDefaultAsync();//this.GetCachedUser();
                supportPaidGames = true;
            }
            else
            {
                supportPaidGames = sender.IsAdmin();
            }
            */
            if (sender != null)
            {
                var platformId = BitcornUtils.GetPlatformId(request.UserPlatformId);
                var player = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).AsNoTracking().FirstOrDefaultAsync();
                if (player == null)
                    return StatusCode(404);
                if (player.IsBanned)
                {
                    return StatusCode(500);
                }

                var changesMade = false;
                var battlegroundsProfile = await _dbContext.BattlegroundsUser.FirstOrDefaultAsync(u => u.UserId == player.UserId && u.HostId == sender.UserId);
                if (battlegroundsProfile == null)
                {
                    battlegroundsProfile = new BattlegroundsUser();
                    battlegroundsProfile.UserId = player.UserId;
                    battlegroundsProfile.HostId = sender.UserId;
                    battlegroundsProfile.IsSub = request.IsSub;
                    _dbContext.Add(battlegroundsProfile);
                    changesMade = true;
                }

                var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.GameId == battlegroundsProfile.CurrentGameId && g.Active);
                if (activeGame == null)
                {
                    activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame != null)
                    {
                        var existingPlayerCount = await _dbContext.BattlegroundsUser.Where(x => x.CurrentGameId == activeGame.GameId).CountAsync();
                        if (existingPlayerCount > activeGame.PlayerLimit)
                        {
                            return StatusCode(420);
                        }

                        bool isInCurrentTournament = false;
                        Tournament tournament = null;
                        if (!string.IsNullOrEmpty(activeGame.TournamentId))
                        {
                            tournament = await _dbContext.Tournament.FirstOrDefaultAsync(x => x.TournamentId == activeGame.TournamentId);

                            if (battlegroundsProfile.CurrentTournamentId == tournament.TournamentId)
                            {

                                isInCurrentTournament = true;
                            }
                            else
                            {
                                battlegroundsProfile.TournamentsPlayed++;
                            }
                        }

                        if (tournament != null && !tournament.JoiningBetweenTournamentGames && tournament.MapIndex > 0)
                        {
                            return StatusCode(420);
                        }

                        if (!activeGame.Started)
                        {

                            if (activeGame.Payin > 0 && supportPaidGames && (tournament == null || tournament != null && !isInCurrentTournament))
                            {
                                //  (request.Payin * request.RewardMultiplier);
                                var txid = await TxUtils.SendToBitcornhub(player, activeGame.Payin, "BITCORNBattlegrounds", "Battlegrounds payin", _dbContext);
                                if (txid != null && txid.TxId != null)
                                {
                                    var txLog = new GameInstanceCornReward();
                                    txLog.TxId = txid.TxId.Value;
                                    txLog.GameInstanceId = activeGame.GameId;
                                    txLog.Type = 1;
                                    _dbContext.GameInstanceCornReward.Add(txLog);
                                    battlegroundsProfile.CurrentGameId = activeGame.GameId;
                                    battlegroundsProfile.CurrentPayin = activeGame.Payin * 0.99m;
                                    changesMade = true;
                                }
                                else
                                {
                                    return StatusCode((int)HttpStatusCode.PaymentRequired);
                                }
                            }
                            else
                            {
                                battlegroundsProfile.CurrentGameId = activeGame.GameId;
                                battlegroundsProfile.CurrentPayin = 0;
                                changesMade = true;
                            }

                            if (changesMade)
                            {
                                if (tournament != null && battlegroundsProfile.CurrentTournamentId != tournament.TournamentId)
                                {
                                    battlegroundsProfile.CurrentTournamentId = tournament.TournamentId;
                                }

                                battlegroundsProfile.IsSub = request.IsSub;

                                await AssignTeam(battlegroundsProfile, activeGame, tournament, player);

                                var aInfo = await GameUtils.GetAvatar(_dbContext, player, GameUtils.AvatarPlatformWindows);
                                var packet = GetJoinPacket(aInfo, player, battlegroundsProfile);
                                var result = await WebSocketsController.TryBroadcastToBattlegroundsUser(sender.UserIdentity.Auth0Id,
                                    _dbContext,
                                    "user-join-game",
                                    packet
                                    );

                                if (result == WebSocketsController.SocketBroadcastResult.Success)
                                {
                                    battlegroundsProfile.VerifiedGameId = activeGame.GameId;
                                }
                            }
                        }
                        else
                        {
                            return new
                            {
                                gameStarted = true
                            };
                        }
                    }
                    else
                    {
                        return new
                        {
                            gameNotFound = true
                        };
                    }
                    if (changesMade)
                    {
                        await _dbContext.SaveAsync();
                    }
                }

                var avatarInfo = await GameUtils.GetAvatar(_dbContext, player, GameUtils.AvatarPlatformWindows);
                return GetJoinPacket(avatarInfo, player, battlegroundsProfile);
                /*
                return new UserJoinGamePacket
                {
                    avatarInfo=avatarInfo,
                    userId = player.UserId,
                    battlegroundsProfile = battlegroundsProfile
                };*/
            }
            throw new ArgumentException();
        }

        private async Task AssignTeam(BattlegroundsUser battlegroundsProfile, GameInstance activeGame, Tournament tournament, User player)
        {
            var gameMode = activeGame.GetGameMode();
            if (gameMode != BattlegroundsGameMode.Raidboss)
            {
                if (activeGame.EnableTeams)
                {
                    bool teamAssigned = false;
                    if (tournament != null && activeGame.EnableTeams)
                    {
                        var history = await _dbContext.BattlegroundsGameHistory
                            .Join(_dbContext.GameInstance, (x => x.GameId), (x => x.GameId), (b, g) => new { b, g })
                            .Where(x => x.b.UserId == player.UserId && x.g.TournamentId == tournament.TournamentId).OrderByDescending(x => x.g.GameId).FirstOrDefaultAsync();

                        if (history != null)
                        {
                            battlegroundsProfile.Team = history.b.Team;
                            teamAssigned = true;
                        }

                    }

                    if (!teamAssigned)
                    {
                        if (activeGame.LastTeamSeed == null)
                        {
                            activeGame.LastTeamSeed = 1;
                        }


                        battlegroundsProfile.Team = activeGame.LastTeamSeed;

                        if (activeGame.LastTeamSeed == 0) activeGame.LastTeamSeed = 1;
                        else activeGame.LastTeamSeed = 0;
                    }

                }
                else
                {
                    battlegroundsProfile.Team = null;
                }
            }
            else
            {
                if (player.UserId == activeGame.HostId)
                {
                    battlegroundsProfile.Team = 0;
                }
                else
                {
                    battlegroundsProfile.Team = 1;

                }
            }
        }

        UserJoinGamePacket GetJoinPacket(UserAvatarOutput avatarInfo, User player, BattlegroundsUser battlegroundsProfile)
        {
            //if (string.IsNullOrEmpty(subTier)) subTier = "0000";
            return new UserJoinGamePacket
            {
                avatarInfo = avatarInfo,
                userId = player.UserId,
                battlegroundsProfile = battlegroundsProfile,
                twitchId = player.UserIdentity.TwitchId,
                twitchUsername = player.UserIdentity.TwitchUsername,
                isSub = battlegroundsProfile.IsSub
            };

        }

        class UserJoinGamePacket
        {
            public UserAvatarOutput avatarInfo { get; set; }
            public int userId { get; set; }
            public BattlegroundsUser battlegroundsProfile { get; set; }
            public string twitchId { get; set; }
            public string twitchUsername { get; set; }
            public bool isSub { get; set; }
        }


        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("start")]
        public async Task<ActionResult<object>> Start()
        {
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value != 0)
            {
                throw new NotImplementedException();
            }

            try
            {
                

                var sender = this.GetCachedUser();
                if (sender != null)
                {
                    var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame != null)
                    {
                        activeGame.Started = true;
                        await _dbContext.SaveAsync();
                        return new
                        {
                            success = true
                        };
                    }
                    else
                    {
                        return new
                        {
                            success = false
                        };
                    }


                }
            }
            catch (Exception ex)
            {
                await BITCORNLogger.LogError(_dbContext, ex, "");
            }

            return new
            {
                success = false
            };
        }

        public class TournamentInfo
        {
            public List<GameSummary> MatchHistoryContainer { get; set; }
            public int MapIndex { get; set; }
            public bool IsComplete { get; set; }
            public bool IsTournament { get; set; }
            public BattlegroundsGameHistoryOutput[] MatchHistorySummary { get; set; }
            public string[] Maps { get; set; }
            public int? WinningTeam { get; set; }
        }

        public class GameSummary
        {
            public GameInstance Game { get; set; }
            public BattlegroundsGameHistoryOutput[] Players { get; set; }
        }

        async Task<TournamentInfo> GetTournamentInfo(GameInstance activeGame)
        {
            Tournament tournament = null;
            var matchHistoryContainer = new List<GameSummary>();

            if (!string.IsNullOrEmpty(activeGame.TournamentId))
            {
                tournament = await _dbContext.Tournament.FirstOrDefaultAsync(x => x.TournamentId == activeGame.TournamentId);
                var allPlayedTournamentGames = await _dbContext.GameInstance.Where(x => x.TournamentId == tournament.TournamentId).ToArrayAsync();
                foreach (var played in allPlayedTournamentGames)
                {
                    var matchHistory = await _dbContext.BattlegroundsGameHistory
                        .Where(x => x.GameId == played.GameId)
                        .Join(_dbContext.UserIdentity, (g) => g.UserId, (u) => u.UserId, (g, u) => new { g, u })
                        .ToArrayAsync();

                    matchHistoryContainer.Add(new GameSummary()
                    {
                        Game = played,
                        Players = matchHistory.Select(x => new BattlegroundsGameHistoryOutput(played, x.g, x.u)).OrderByDescending(x => x.Points).ToArray(),

                    });
                }

                Dictionary<int, List<BattlegroundsGameHistoryOutput>> summed = new Dictionary<int,
                    List<BattlegroundsGameHistoryOutput>>();

                for (int i = 0; i < matchHistoryContainer.Count; i++)
                {
                    var m = matchHistoryContainer[i];
                    foreach (var item in m.Players)
                    {
                        if (!summed.TryGetValue(item.UserId, out var history))
                        {
                            summed.Add(item.UserId, new List<BattlegroundsGameHistoryOutput>() { item });
                        }
                        else
                        {
                            history[0].Add(item);
                            history.Add(item);
                        }
                    }
                }

                /*
                foreach (var item in summed.Values)
                {
                    if (item.Count > 0)
                    {
                        item[0].Placement = item.Last().Placement;
                    }
                }
                */
                TournamentData tournamentData = null;
                if (tournament != null && !string.IsNullOrEmpty(tournament.TournamentData))
                {
                    try
                    {
                        tournamentData = JsonConvert.DeserializeObject<TournamentData>(tournament.TournamentData);
                    }
                    catch
                    {

                    }
                }

                var matchHistorySummary = summed.Values.Select(x => x[0]).OrderByDescending(x => x.Points).ToArray();
                int? winningTeam = null;
                if (activeGame.EnableTeams)
                {
                    //var points = new List<int>();
                    var outputs = new Dictionary<int, List<BattlegroundsGameHistoryOutput>>();
                    //var allTeams = new List<int>();
                    foreach (var item in matchHistorySummary)
                    {
                        var idx = item.Team.Value;

                        if (!outputs.TryGetValue(idx, out var list))
                        {
                            list = new List<BattlegroundsGameHistoryOutput>();
                            outputs.Add(idx, list);
                        }

                        list.Add(item);

                    }

                    var final = new List<BattlegroundsGameHistoryOutput>();
                    if (outputs.Values.Count > 0)
                    {
                        var orderedSums = outputs.Values.OrderByDescending(x => x.Sum(s => s.Points));
                        foreach (var item in orderedSums)
                        {
                            if (winningTeam == null)
                            {
                                if (item.Count > 0)
                                {
                                    winningTeam = item[0].Team;
                                }
                            }
                            final.AddRange(item.OrderByDescending(x => x.Points));
                        }

                        matchHistorySummary = final.ToArray();

                    }
                    /*
                    foreach (var item in outputs)
                    {
                        var points = item.Value.Select(x=>x.Points).Sum();
                   
                    }
                    */

                    //winningTeam = 0;


                    /*
                    if (points.Count < idx)
                    {
                        var diff = Math.Abs(points.Count - idx);
                        for (int i = 0; i < diff + 1; i++)
                        {
                            points.Add(0);
                        }
                    }

                    points[idx] += item.Points;
                    */
                    /*if(!allTeams.Contains(item.Team.Value))
                    {
                        allTeams.Add(item.Team.Value);
                    }*/
                    /*int bestPoints = -1;
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (points[i] > bestPoints)
                        {
                            bestPoints = points[i];
                            winningTeam = i;
                        }
                    }*/
                }
                return new TournamentInfo()
                {
                    MatchHistoryContainer = matchHistoryContainer,
                    MatchHistorySummary = matchHistorySummary,
                    MapIndex = tournament.MapIndex,
                    IsComplete = tournament.Completed,
                    IsTournament = true,
                    Maps = tournamentData != null ? tournamentData.Maps : new string[0],
                    WinningTeam = winningTeam
                };
            }
            else
            {
                return new TournamentInfo()
                {
                    MatchHistoryContainer = new List<GameSummary>()
                };
            }
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("pickupcorn")]
        public async Task<ActionResult<object>> PickupCorn([FromBody] PickupCornRequest request)
        {
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value != 0)
            {
                throw new NotImplementedException();
            }


            try
            {

                var sender = this.GetCachedUser();
                if (sender != null)
                {
                    var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame != null)
                    {
                        var recipient = await _dbContext.JoinUserModels().FirstOrDefaultAsync(x => x.UserId == request.UserId);
                        if (recipient != null)
                        {
                            var signedTx = await TxUtils.ClaimSignedTx(_dbContext, recipient, request.Key, activeGame);
                            if (signedTx != null && signedTx.Value.Item2.Tx != null)
                            {
                                var senderProfile = await _dbContext.BattlegroundsUser.FirstOrDefaultAsync(x => x.UserId == sender.UserId && x.HostId == sender.UserId);
                                senderProfile.HostCornRewards += signedTx.Value.Item1.Amount * 0.01m;
                                await _dbContext.SaveAsync();
                            }
                            return signedTx;
                        }
                    }
                }

                return StatusCode(404);

            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(request));
                throw e;
            }
        }
        class TournamentData
        {
            public string[] Maps { get; set; }
        }

        async Task<UserJoinGamePacket[]> GetPlayers(GameInstance activeGame)
        {
            var players = await _dbContext.JoinUserModels()
                            .Join(_dbContext.BattlegroundsUser, (u) => u.UserId, (b) => b.UserId, (u, b) => new { u, b }).Where(x => x.b.CurrentGameId == activeGame.GameId && x.b.HostId == activeGame.HostId).ToArrayAsync();
            //var players = await _dbContext.BattlegroundsUser.Where(u => u.CurrentGameId == activeGame.GameId).ToArrayAsync();
            //var twitchIds = await _dbContext.JoinUserModels().Where(u => playerIds.Contains(u.UserId)).Select(u => u.UserIdentity.TwitchId).ToArrayAsync();
            var userIds = players.Select(x => x.u.UserId).ToArray();
            var avatars = await GameUtils.GetAvatars(_dbContext, userIds, GameUtils.AvatarPlatformWindows);
            return players.Select(x => GetJoinPacket(avatars[x.u.UserId], x.u, x.b)).ToArray();
        }

        async Task<(Tournament, GameInstance, bool)?> GetExistingGame(User sender)
        {
            if (sender == null) return null;

            Tournament existingTournament = await GetActiveTournamentForHost(sender);
            bool needsGameAlloc = false;
            GameInstance activeGame = null;
            if (existingTournament != null)
            {
                activeGame = await GetActiveGameForHost(sender);
                if (activeGame == null)
                {
                    //activeGame = await GetPreviousGame(existingTournament);
                    needsGameAlloc = true;
                }
            }
            else
            {
                if (activeGame == null)
                {
                    activeGame = await GetActiveGameForHost(sender);

                }
            }

            return (existingTournament, activeGame, needsGameAlloc);
            /*
            
            if (activeGame == null)
            {
                activeGame = await GetActiveGameForHost(sender);//_dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                if (activeGame == null) return null;
            }


            var players = await GetPlayers(activeGame);
            //GetJoinPacket
            bool isTournament = !string.IsNullOrEmpty(activeGame.TournamentId);
            if (isTournament)
                existingTournament = await GetActiveTournamentForHost(sender);//_dbContext.Tournament.Where(x => x.TournamentId == activeGame.TournamentId).FirstOrDefaultAsync();
            if (players.Length > 0 || isTournament)
            {

                return CreateGameResponse(existingTournament, activeGame, sender, players, false);
            }

            return null;
            */
        }

        bool CheckPaidSupport() => SUPPORT_PAID_GAMES;

        const bool SUPPORT_PAID_GAMES = true;
        [HttpGet("version")]
        public async Task<ActionResult<object>> Version()
        {
            return new  { 
                version = 1,
                supportsPayin = CheckPaidSupport()
            };
        }

            [ServiceFilter(typeof(LockUserAttribute))]
        [HttpGet("existinggame")]
        public async Task<ActionResult<object>> ExistingGame()
        {
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value != 0)
            {
                throw new NotImplementedException();
            }

            try
            {
                var sender = this.GetCachedUser();
                if (sender != null)
                {
                    var result = await GetExistingGame(sender);
                    if (result != null)
                    {
                        var (existingTournament, activeGame, needsGameAlloc) = result.Value;
                        if (needsGameAlloc)
                        {
                            if (activeGame != null) throw new NotImplementedException("logic error");
                            activeGame = InitSimpleInstance(sender);
                            _dbContext.GameInstance.Add(activeGame);
                            if (activeGame.GetGameMode() == BattlegroundsGameMode.Raidboss)
                            {
                                var senderProfile = await _dbContext.BattlegroundsUser.Where(x => x.HostId == sender.UserId && x.UserId == sender.UserId).FirstOrDefaultAsync();
                                if (senderProfile != null)
                                {
                                    senderProfile.CurrentGameId = activeGame.GameId;
                                    senderProfile.VerifiedGameId = activeGame.GameId;
                                    senderProfile.Team = 0;
                                }
                            }
                            await _dbContext.SaveAsync();

                            return await ProgressTournamentWrapper(existingTournament, activeGame, sender, activeGame.GetGameMode() == BattlegroundsGameMode.Raidboss);
                        }
                        if (activeGame != null)
                        {
                            bool isTournament = existingTournament != null;
                            UserJoinGamePacket[] players = new UserJoinGamePacket[0];

                            players = await GetPlayers(activeGame);


                            if (players.Length > 0 || isTournament)
                            {

                                var resp = await CreateGameResponse(existingTournament, activeGame, sender, players, false);
                                return resp;
                            }
                        }
                        /*
                        //GetJoinPacket
                        if (isTournament)
                            existingTournament = await GetActiveTournamentForHost(sender);//_dbContext.Tournament.Where(x => x.TournamentId == activeGame.TournamentId).FirstOrDefaultAsync();
                       */
                    }

                }

            }
            catch (Exception ex)
            {
                await BITCORNLogger.LogError(_dbContext, ex, "");
                throw ex;
            }
            return StatusCode(404);
        }


        async Task<GameInstance> GetActiveGameForHost(User sender)
        {
            return await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
        }
        async Task UpdatePreviousGame(Tournament existingTournament, GameInstance activeGame)
        {
            var sql = $" update [{nameof(Tournament)}] set [{nameof(Tournament.PreviousMapId)}] = {activeGame.GameId} where [{nameof(Tournament.TournamentId)}] = '{existingTournament.TournamentId}'";
            await DbOperations.ExecuteSqlRawAsync(_dbContext, sql);


        }



        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("create")]
        public async Task<ActionResult<object>> Create([FromBody] BattlegroundsCreateGameRequest request)
        {
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value != 0)
            {
                throw new NotImplementedException();
            }

            try
            {
                var sender = this.GetCachedUser();
                if (sender != null)
                {
                    var senderProfile = await _dbContext.BattlegroundsUser.Where(x => x.UserId == sender.UserId && x.HostId == sender.UserId).FirstOrDefaultAsync();
                    if (senderProfile == null)
                    {
                        senderProfile = new BattlegroundsUser();
                        senderProfile.UserId = sender.UserId;
                        senderProfile.HostId = sender.UserId;
                        senderProfile.IsSub = true;
                        _dbContext.Add(senderProfile);
                        await _dbContext.SaveAsync();
                    }

                    var result = await GetExistingGame(sender);
                    var (existingTournament, activeGame, needsToProgressTournament) = result.Value;
                    if (!needsToProgressTournament && activeGame != null)
                    {
                        return await CreateGameResponse(existingTournament, activeGame, sender, true, false);
                    }

                    activeGame = CreateGameInstance(request, sender);
                    async Task UpdateSenderToCurrent()
                    {
                        if (activeGame.GetGameMode() == BattlegroundsGameMode.Raidboss)
                        {

                            await _dbContext.SaveAsync();
                            senderProfile.CurrentGameId = activeGame.GameId;
                            senderProfile.VerifiedGameId = activeGame.GameId;
                            senderProfile.Team = 0;
                            await _dbContext.SaveAsync();
                        }
                    }

                    _dbContext.GameInstance.Add(activeGame);


                    if (!needsToProgressTournament)
                    {
                        await _dbContext.SaveAsync();


                        if (request.Tournament)
                        {
                            if (existingTournament != null) throw new NotImplementedException("logic error");


                            //if (existingTournament == null)

                            existingTournament = CreateTournament(request, sender, activeGame);
                            _dbContext.Tournament.Add(existingTournament);


                            activeGame.TournamentId = existingTournament.TournamentId;
                        }

                        await _dbContext.SaveAsync();
                        await UpdateSenderToCurrent();
                        return await CreateGameResponse(existingTournament, activeGame, sender, activeGame.GetGameMode() == BattlegroundsGameMode.Raidboss, true);
                    }
                    else
                    {

                        await UpdateSenderToCurrent();
                        return await ProgressTournamentWrapper(existingTournament, activeGame, sender, activeGame.GetGameMode() == BattlegroundsGameMode.Raidboss);
                        //}
                    }
              
                }
                else
                    return StatusCode((int)HttpStatusCode.Forbidden);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);

                throw e;
            }
        }

        private async Task<object> ProgressTournamentWrapper(Tournament existingTournament, GameInstance activeGame, User sender, bool forceFetchPlayers)
        {
            //if (existingTournament != null)
            //{
            if (existingTournament == null)
            {
                throw new NotImplementedException("logic error 2");
            }

            activeGame.TournamentId = existingTournament.TournamentId;
            await ProgressTournament(existingTournament, sender, activeGame);

            bool fetchPlayers = await MigratePlayersToNextGame(existingTournament, activeGame, sender);
            if (forceFetchPlayers) fetchPlayers = true;
            await _dbContext.SaveAsync();

            await UpdatePreviousGame(existingTournament, activeGame);

            return await CreateGameResponse(existingTournament, activeGame, sender, fetchPlayers, true);
        }

        private async Task<object> CreateGameResponse(Tournament existingTournament, GameInstance activeGame, User sender, bool fetchPlayers, bool newGame)
        {
            return await CreateGameResponse(existingTournament, activeGame, sender, fetchPlayers ? await GetPlayers(activeGame) : new UserJoinGamePacket[0], newGame);

        }

        private async Task<object> CreateGameResponse(Tournament existingTournament, GameInstance activeGame, User sender, UserJoinGamePacket[] players, bool newGame)
        {

            return new
            {
                IsNewGame = newGame,
                Players = players,
                ActiveGame = activeGame,
                TournamentInfo = await GetTournamentInfo(activeGame),

                JoiningBetweenTournamentGames = existingTournament != null ? existingTournament.JoiningBetweenTournamentGames : true

            };
        }


        private async Task<bool> MigratePlayersToNextGame(Tournament existingTournament, GameInstance activeGame, User sender)
        {
            bool fetchPlayers = false;
            if (existingTournament != null && !existingTournament.JoiningBetweenTournamentGames && existingTournament.MapIndex > 0)
            {
                var ps = await _dbContext.BattlegroundsUser.Where(x => x.HostId == sender.UserId && x.CurrentGameId == existingTournament.PreviousMapId).ToArrayAsync();
                int updateCount = 0;
                foreach (var item in ps)
                {
                    item.CurrentGameId = activeGame.GameId;
                    updateCount++;
                }
                await _dbContext.SaveAsync();

                if (updateCount > 0)
                {
                    fetchPlayers = true;
                }

            }

            return fetchPlayers;
        }

        async Task<GameInstance> GetPreviousGame(Tournament existingTournament)
        {
            return await _dbContext.GameInstance.Where(x => x.GameId == existingTournament.PreviousMapId).FirstOrDefaultAsync();
        }

        private async Task ProgressTournament(Tournament existingTournament, User sender, GameInstance activeGame)
        {
            //if (existingTournament.MapIndex > 0)
            {
                var previousGame = await GetPreviousGame(existingTournament);
                if (previousGame != null)
                {
                    activeGame.CopySettings(previousGame);
                }
            }


            existingTournament.MapIndex++;
            try
            {
                var tData = JsonConvert.DeserializeObject<TournamentData>(existingTournament.TournamentData);
                activeGame.MapId = tData.Maps[existingTournament.MapIndex];
            }
            catch
            {

            }
        }

        private async Task<Tournament> GetActiveTournamentForHost(User sender)
        {
            return await _dbContext.Tournament.Where(x => x.UserId == sender.UserId && !x.Completed).FirstOrDefaultAsync();
        }

        private Tournament CreateTournament(BattlegroundsCreateGameRequest request, User sender, GameInstance activeGame = null)
        {
            var existingTournament = new Tournament();
            existingTournament.UserId = sender.UserId;
            existingTournament.TournamentId = Guid.NewGuid().ToString();
            existingTournament.MapIndex = 0;
            existingTournament.MapCount = request.TournamentMaps.Length;
            existingTournament.PointMethod = request.TournamentPointMethod.HasValue ? request.TournamentPointMethod.Value : 0;
            if (activeGame != null)
            {
                existingTournament.PreviousMapId = activeGame.GameId;
            }
            existingTournament.StartTime = DateTime.Now;
            existingTournament.JoiningBetweenTournamentGames = request.JoiningBetweenTournamentGames;
            existingTournament.TournamentData = JsonConvert.SerializeObject(new TournamentData()
            {
                Maps = request.TournamentMaps
            });

            if (existingTournament.TournamentData.Length > 500) throw new ArgumentException();

            return existingTournament;
        }

        GameInstance InitSimpleInstance(User sender)
        {
            var activeGame = new GameInstance();
            activeGame.Active = true;
            activeGame.HostId = sender.UserId;
            activeGame.Started = false;
            activeGame.RewardMultiplier = 1;//request.RewardMultiplier;
            return activeGame;
        }

        GameInstance CreateGameInstance(BattlegroundsCreateGameRequest request, User sender)
        {
            var activeGame = InitSimpleInstance(sender);
            activeGame.Payin = request.Payin;
            activeGame.Reward = 0;//request.Reward;
            activeGame.Bgrains = request.Bgrains;
            //     activeGame.HostDebitCornTxId = txid;
            activeGame.GameMode = request.GameMode;
            activeGame.PlayerLimit = request.MaxPlayerCount;
            if(activeGame.PlayerLimit>10000)
            {
                activeGame.PlayerLimit = 10000;
            }

            activeGame.EnableTeams = request.EnableTeams;
            if (activeGame.GetGameMode() == BattlegroundsGameMode.Raidboss)
            {
                activeGame.EnableTeams = true;
                activeGame.Payin = 0;
            }
            
            if(!CheckPaidSupport())
            {
                activeGame.Bgrains = false;
                activeGame.Payin = 0;
            }

            if(activeGame.Payin > 100_000_000)
            {
                activeGame.Payin = 100_000_000;
            }

            activeGame.MapId = request.MapId;
            activeGame.Data = request.Data;
            if (activeGame.Data.Length > 500)
            {
                throw new ArgumentException();
            }

            return activeGame;
        }

        public static decimal[] SplitReward(decimal value, decimal startDistribution, int count)
        {
            decimal[] output = new decimal[count];
            decimal distribution = startDistribution;

            decimal left = value;

            for (int i = 0; i < count; i++)
            {
                var amount = distribution * value;
                if (left > amount)
                {
                    left -= amount;
                }
                else
                {
                    amount = left;
                    left = 0;
                }

                output[i] = amount;
                distribution /= 2;
                if (distribution <= 0)
                {
                    break;
                }
                if (left <= 0)
                    break;
            }
            if (left > 0)
            {
                output[0] += left;
            }
            return output;
        }
        async Task<decimal> SendfromBitcornhubTransaction(User user, int gameId, decimal amount, string context)
        {
            if (amount <= 0)
                return 0;
            var receipt = await TxUtils.SendFromBitcornhubGetReceipt(user, amount, "BITCORNBattlegrounds", context, _dbContext);
            if (receipt != null)
            {
                if (receipt.TxId != null)
                {
                    var link = new GameInstanceCornReward();
                    link.GameInstanceId = gameId;
                    link.TxId = receipt.TxId.Value;
                    link.Type = 0;
                    _dbContext.GameInstanceCornReward.Add(link);
                    return amount;
                }
            }
            return 0;
        }



        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpGet("abandon")]
        public async Task<ActionResult<object>> AbandonGame()
        {
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value != 0)
            {
                throw new NotImplementedException();
            }

            var sender = this.GetCachedUser();
            if (sender != null)
            {
                var activeGame = await GetActiveGameForHost(sender);//_dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                if (activeGame != null && activeGame.Active)
                {

                    Tournament tournament = null;
                    if (!string.IsNullOrEmpty(activeGame.TournamentId))
                    {
                        tournament = await _dbContext.Tournament.FirstOrDefaultAsync(x => x.TournamentId == activeGame.TournamentId);
                        if (tournament != null)
                        {
                            tournament.Completed = true;
                        }
                    }

                    activeGame.Active = false;
                    await _dbContext.SaveAsync();
                    int[] inTransactions = null;
                    if (tournament == null)
                    {
                        inTransactions = await _dbContext.GameInstanceCornReward.Where(x => x.GameInstanceId == activeGame.GameId).Select(x => x.TxId).ToArrayAsync();//tournamentInfo.MatchHistorySummary.Length * activeGame.Payin;

                    }
                    else
                    {
                        inTransactions = await _dbContext.GameInstanceCornReward.Join(_dbContext.GameInstance, (x => x.GameInstanceId), (x => x.GameId),
                                       (r, g) => new { r, g }).Where(x => x.g.TournamentId == tournament.TournamentId && x.r.Type == 1).Select(x => x.r.TxId).ToArrayAsync();//tournamentInfo.MatchHistorySummary.Length * activeGame.Payin;

                    }
                    if (inTransactions.Length > 0)
                    {
                        var cornTxs = await _dbContext.CornTx.Where(x => inTransactions.Contains(x.CornTxId)).ToArrayAsync();
                        //await _dbContext.GameInstanceCornReward.Where(x=>inTransactions.Contains(x.TxId)).ToArrayAsync();
                        if (cornTxs.Length > 0)
                        {
                            var recipientIds = cornTxs.Select(x => x.SenderId.Value).ToArray();
                            var recipientUsers = await _dbContext.JoinUserModels().Where(x => recipientIds.Contains(x.UserId)).Select(x => "userid|" + x.UserId).ToArrayAsync();
                            if (recipientUsers.Length > 0)
                            {
                                var bitcornhub = await TxUtils.GetBitcornhub(_dbContext);
                                TxProcessInfo txInfo = await TxUtils.ProcessRequest(new TxRequest(bitcornhub, activeGame.Payin * 0.99m, "BITCORNBattlegrounds", "Battlegrounds payin refund", recipientUsers), _dbContext);
                                var sql = new StringBuilder();
                                if (txInfo.WriteTransactionOutput(sql))
                                {
                                    await DbOperations.ExecuteSqlRawAsync(_dbContext, sql.ToString());
                                    await _dbContext.SaveAsync(IsolationLevel.RepeatableRead);
                                    foreach (var receipt in txInfo.Transactions)
                                    {
                                        var link = new GameInstanceCornReward();
                                        link.GameInstanceId = activeGame.GameId;
                                        link.TxId = receipt.TxId.Value;
                                        link.Type = 2;
                                        _dbContext.GameInstanceCornReward.Add(link);
                                    }

                                    await _dbContext.SaveAsync();
                                    return StatusCode(200);
                                }
                            }
                        }
                    }
                }
            }

            return StatusCode(404);

        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("processgame")]
        public async Task<ActionResult<object>> ProcessGame([FromBody] BattlegroundsProcessGameRequest request)
        {
            var userMode = this.GetUserMode();

            if (userMode != null && userMode.Value != 0)
            {
                throw new NotImplementedException();
            }

            try
            {
                Tournament tournament = null;
                var sender = this.GetCachedUser();
                if (sender != null)
                {
                    var activeGame = await GetActiveGameForHost(sender);//_dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame != null)
                    {
                        var senderProfile = await _dbContext.BattlegroundsUser
                                  .Where(x => x.UserId == sender.UserId && x.HostId == sender.UserId).FirstOrDefaultAsync();

                        if (!string.IsNullOrEmpty(activeGame.TournamentId))
                        {
                            tournament = await _dbContext.Tournament.Where(x => x.TournamentId == activeGame.TournamentId).FirstOrDefaultAsync();
                            if (tournament != null && !tournament.Completed)
                            {
                                if (tournament.MapIndex >= tournament.MapCount - 1)
                                {
                                    tournament.Completed = true;
                                }

                            }
                        }

                        var playerIds = await _dbContext.BattlegroundsUser.Where(u => u.CurrentGameId == activeGame.GameId).Select(u => u.UserId).ToArrayAsync();
                        var twitchIds = await _dbContext.JoinUserModels().Where(u => playerIds.Contains(u.UserId)).Select(u => u.UserIdentity.TwitchId).ToArrayAsync();


                        activeGame.Active = false;

                        //map players to their ids
                        var playerUpdates = request.Players.ToDictionary(u => u.UserId, u => u);
                        //var ids = playerUpdates.Keys.ToHashSet();
                        //select registered users from database
                        var users = await _dbContext.JoinUserModels().Where(p => playerIds.Contains(p.UserId)).AsNoTracking().ToDictionaryAsync(u => u.UserId, u => u);
                        //var existingUserIds = users.Keys.ToArray();

                        var allStats = _dbContext.BattlegroundsUser.Where(p => playerIds.Contains(p.UserId) && p.HostId == sender.UserId).ToDictionary(u => u.UserId, u => u);

                        var rewards = new List<(string, decimal)>();//new decimal[0];
                        /*
						if (activeGame.Reward > 0)
						{
							rewards = SplitReward(activeGame.Reward, .5m, request.Players.Length);
						}*/
                        /*
                        if (activeGame.Payin > 0)
                        {
                            rewards = new decimal[] {
                                //(activeGame.Payin*activeGame.RewardMultiplier)*users.Count
                                activeGame.Payin * playerIds.Length
                            };

                        }*/
                        var orderedUsers = new List<User>();
                        int? winningTeam = null;
                        for (int i = 0; i < request.Players.Length; i++)
                        {
                            var userId = request.Players[i].UserId;
                            var history = new BattlegroundsGameHistory();
                            history.Add(request.Players[i]);
                            history.GameId = activeGame.GameId;
                            history.Placement = i;
                            history.UserId = request.Players[i].UserId;

                            if (allStats.TryGetValue(userId, out var statUser))
                            {
                                history.Team = statUser.Team;
                            }


                            if (tournament != null)
                            {
                                if (tournament.PointMethod == 0)
                                {
                                    history.Points = (request.Players.Length - i) + 1;
                                }
                                else
                                {
                                    history.Points = 0;
                                }
                            }
                            else
                            {
                                history.Points = 0;
                            }
                            _dbContext.BattlegroundsGameHistory.Add(history);

                            if (users.TryGetValue(userId, out User user))
                            {
                                orderedUsers.Add(user);
                                /*if (allStats.TryGetValue(userId, out BattlegroundsUser stats) && i == 0)
                                {
                                    stats.Wins++;
                                }
                                */

                            }
                        }

                        foreach (var player in allStats.Values)
                        {
                            player.GamesPlayed++;
                            player.Add(playerUpdates[player.UserId]);
                        }

                        if (tournament != null)
                        {

                            if (tournament.Completed)
                            {
                                await _dbContext.SaveAsync();
                                var tournamentInfo = await GetTournamentInfo(activeGame);
                                decimal rewardFull = 0;
                                if (activeGame.Payin > 0)
                                {
                                    var inTransactions = await _dbContext.GameInstanceCornReward.Join(_dbContext.GameInstance, (x => x.GameInstanceId), (x => x.GameId),
                                        (r, g) => new { r, g }).Where(x => x.g.TournamentId == tournament.TournamentId && x.r.Type == 1).Select(x => x.r.TxId).ToArrayAsync();//tournamentInfo.MatchHistorySummary.Length * activeGame.Payin;

                                    rewardFull = await _dbContext.CornTx.Where(x => inTransactions.Contains(x.CornTxId)).Select(x => x.Amount.Value).SumAsync();
                                    //var rewardFull = 0;
                                }
                                //if (rewardFull > 0)
                                {
                                    if (!tournament.Completed)
                                    {
                                        rewardFull = 0;
                                    }

                                    var rewardToPlayer = rewardFull * 0.99m;
                                    var rewardToHost = rewardFull * 0.01m;

                                    if (!activeGame.EnableTeams)
                                    {
                                        var winner = tournamentInfo.MatchHistorySummary.FirstOrDefault();
                                        if (winner != null)
                                        {
                                            var winnerUser = await _dbContext.JoinUserModels().Where(x => x.UserId == winner.UserId).FirstOrDefaultAsync();
                                            var winnerStats = await _dbContext.BattlegroundsUser.Where(x => x.UserId == winner.UserId && x.HostId == activeGame.HostId).FirstOrDefaultAsync();
                                            winnerStats.TournamentWins++;
                                            if (rewardFull > 0)
                                            {
                                                var rewardAmount = await SendfromBitcornhubTransaction(winnerUser, activeGame.GameId, rewardToPlayer,
                                                           "Battlegrounds reward");

                                                rewards.Add((winnerUser.UserIdentity.TwitchUsername, rewardAmount));
                                                winnerStats.TotalCornRewards += rewardAmount;

                                                await SendfromBitcornhubTransaction(sender, activeGame.GameId, rewardToHost,
                                                    "Battlegrounds host reward");

                                                senderProfile.HostCornRewards += rewardToHost;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var ids = tournamentInfo.MatchHistorySummary.Select(x => x.UserId).ToArray();
                                        if (ids.Length > 0)
                                        {

                                            winningTeam = tournamentInfo.WinningTeam;
                                            /*
                                            winningTeam = 1;
                                            if (points[0] > points[1])
                                            {
                                                winningTeam = 0;
                                            }*/



                                            var allParticipants = await _dbContext.JoinUserModels().Where(x => ids.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId, x => x);
                                            if (allParticipants.Count > 0)
                                            {
                                                var allProfiles = await _dbContext.BattlegroundsUser.Where(x => ids.Contains(x.UserId) && x.Team == winningTeam && x.HostId == activeGame.HostId).ToDictionaryAsync(x => x.UserId, x => x);
                                                var rewardChunk = rewardToPlayer / allProfiles.Count;
                                                foreach (var item in tournamentInfo.MatchHistorySummary)
                                                {
                                                    if (allProfiles.TryGetValue(item.UserId, out var bgInfo))
                                                    {
                                                        if (allParticipants.TryGetValue(item.UserId, out var userInfo))

                                                        {
                                                            bgInfo.TournamentTeamWins++;
                                                            if (rewardFull > 0)
                                                            {
                                                                var rewardAmount = await SendfromBitcornhubTransaction(userInfo, activeGame.GameId, rewardChunk,
                                                                "Battlegrounds reward");


                                                                rewards.Add((userInfo.UserIdentity.TwitchUsername, rewardAmount));
                                                            }
                                                        }
                                                    }
                                                }

                                                if (rewardFull > 0)
                                                {
                                                    await SendfromBitcornhubTransaction(sender, activeGame.GameId, rewardToHost,
                                                      "Battlegrounds host reward");

                                                    senderProfile.HostCornRewards += rewardToHost;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (activeGame.EnableTeams)
                                {
                                    if (allStats.Count > 0 && orderedUsers.Count > 0)
                                    {
                                        var winnerStats = allStats[orderedUsers[0].UserId];

                                        winningTeam = winnerStats.Team;
                                        for (int i = 0; i < orderedUsers.Count; i++)
                                        {
                                            if (allStats.TryGetValue(orderedUsers[i].UserId, out BattlegroundsUser stats))
                                            {
                                                if (winnerStats.Team == stats.Team && stats.Team != null)
                                                {
                                                    stats.TeamWins++;
                                                    //winners.Add((stats, user));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (orderedUsers.Count > 0 && allStats.Count > 0)
                            {
                                var winnerStats = allStats[orderedUsers[0].UserId];

                                var user = orderedUsers[0];
                                decimal rewardFull = 0;//playerIds.Length * activeGame.Payin;//rewards[0];
                                                       // if (rewardFull > 0)
                                if (activeGame.Payin > 0)
                                {
                                    var inTransactions = await _dbContext.GameInstanceCornReward.Where(x => x.GameInstanceId == activeGame.GameId).Select(x => x.TxId).ToArrayAsync();//tournamentInfo.MatchHistorySummary.Length * activeGame.Payin;
                                    rewardFull = await _dbContext.CornTx.Where(x => inTransactions.Contains(x.CornTxId)).Select(x => x.Amount.Value).SumAsync();

                                }

                                {
                                    var rewardToPlayer = rewardFull * 0.99m;
                                    var rewardToHost = rewardFull * 0.01m;
                                    if (!activeGame.EnableTeams)
                                    {
                                        winnerStats.Wins++;
                                        if (rewardFull > 0)
                                        {
                                            var rewardAmount = await SendfromBitcornhubTransaction(user, activeGame.GameId, rewardToPlayer,
                                                "Battlegrounds reward");

                                            winnerStats.TotalCornRewards += rewardAmount;
                                            rewards.Add((user.UserIdentity.TwitchUsername, rewardAmount));
                                            await SendfromBitcornhubTransaction(sender, activeGame.GameId, rewardToHost,
                                                "Battlegrounds host reward");

                                            senderProfile.HostCornRewards += rewardToHost;
                                            //rewards[0] = rewardAmount;
                                        }
                                    }
                                    else
                                    {
                                        var winners = new List<(BattlegroundsUser, User)>();

                                        winningTeam = winnerStats.Team;
                                        for (int i = 0; i < orderedUsers.Count; i++)
                                        {
                                            if (allStats.TryGetValue(orderedUsers[i].UserId, out BattlegroundsUser stats))
                                            {
                                                if (winnerStats.Team == stats.Team && stats.Team != null)
                                                {
                                                    stats.TeamWins++;
                                                    winners.Add((stats, orderedUsers[i]));
                                                }
                                            }
                                        }

                                        var rewardChunk = rewardToPlayer / winners.Count;
                                        for (int i = 0; i < winners.Count; i++)
                                        {
                                            if (rewardChunk > 0)
                                            {
                                                var rAmount = await SendfromBitcornhubTransaction(winners[i].Item2, activeGame.GameId, rewardChunk,
                                          "Battlegrounds reward");

                                                winners[i].Item1.TotalCornRewards += rAmount;
                                                rewards.Add((winners[i].Item2.UserIdentity.TwitchUsername, rAmount));
                                            }
                                        }

                                        if (rewardToHost > 0)
                                        {
                                            await SendfromBitcornhubTransaction(sender, activeGame.GameId, rewardToHost,
                                          "Battlegrounds host reward");

                                            senderProfile.HostCornRewards += rewardToHost;
                                        }
                                    }
                                }

                            }
                        }

                        if (allStats.Count > 0 || !activeGame.Active)
                        {
                            await _dbContext.SaveAsync();
                        }
                        if (!activeGame.Active && allStats.Count == 0)
                        {
                            await _dbContext.SaveAsync();

                        }
                        //activeGame.EnableTeams
                        //if(allStats.Count == 0 && !activeGame.Active)
                        return new
                        {
                            winningTeam,
                            enableTeams = activeGame.EnableTeams,
                            rewards = rewards.Select(x => new { twitchUsername = x.Item1, amount = x.Item2 }).ToArray(),
                            tournamentInfo = await GetTournamentInfo(activeGame)
                        };
                    }
                    return StatusCode((int)HttpStatusCode.Forbidden);


                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext, e, "");
                System.Diagnostics.Debug.WriteLine(e.Message + "::" + e.StackTrace);
                throw e;
            }
            throw new NotImplementedException();
        }
    }
}