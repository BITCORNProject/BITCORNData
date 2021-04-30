using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Games;
using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [HttpGet("leaderboard/{host}/{orderby}")]
        public async Task<ActionResult<object>> Leaderboard([FromRoute] string host, [FromRoute] string orderby)
        {
            var properties = typeof(BattlegroundsUser)
                .GetProperties()
                .Select(p => p.Name.ToLower())
                .ToArray();
            if (int.TryParse(host, out int userId))
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
                    _dbContext.Add(battlegroundsProfile);
                    changesMade = true;
                }

                var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.GameId == battlegroundsProfile.CurrentGameId && g.Active);
                if (activeGame == null)
                {
                    activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame != null)
                    {
                        if (!activeGame.Started)
                        {
                            if (activeGame.Payin > 0 && supportPaidGames)
                            {
                                //  (request.Payin * request.RewardMultiplier);
                                var txid = await TxUtils.SendToBitcornhub(player, activeGame.Payin, "BITCORNBattlegrounds", "Battlegrounds payin", _dbContext);
                                if (txid != null)
                                {
                                    battlegroundsProfile.CurrentGameId = activeGame.GameId;
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
                                changesMade = true;
                            }

                            if (changesMade)
                            {
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

        UserJoinGamePacket GetJoinPacket(UserAvatarOutput avatarInfo, User player, BattlegroundsUser battlegroundsProfile)
        {
            return new UserJoinGamePacket
            {
                avatarInfo = avatarInfo,
                userId = player.UserId,
                battlegroundsProfile = battlegroundsProfile,
                twitchId = player.UserIdentity.TwitchId,
                twitchUsername = player.UserIdentity.TwitchUsername
            };

        }

        class UserJoinGamePacket
        {
            public UserAvatarOutput avatarInfo { get; set; }
            public int userId { get; set; }
            public BattlegroundsUser battlegroundsProfile { get; set; }
            public string twitchId { get; set; }
            public string twitchUsername { get; set; }
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("join")]
        public async Task<ActionResult<object>> Join([FromBody] BattlegroundsJoinGameRequest request)
        {
            var sender = this.GetCachedUser();
            if (sender != null && sender.IsAdmin())
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
                    _dbContext.Add(battlegroundsProfile);
                    changesMade = true;
                }

                var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.GameId == battlegroundsProfile.CurrentGameId && g.Active);
                if (activeGame == null)
                {
                    activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame != null)
                    {

                        if (activeGame.Payin > 0)
                        {
                            var txid = await TxUtils.SendToBitcornhub(player, activeGame.Payin, "BITCORNBattlegrounds", "Battlegrounds payin", _dbContext);
                            if (txid != null)
                            {
                                battlegroundsProfile.CurrentGameId = activeGame.GameId;

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
                            changesMade = true;
                        }

                    }

                    if (changesMade)
                    {
                        await _dbContext.SaveAsync();
                    }
                }
                var avatarInfo = await GameUtils.GetAvatar(_dbContext, player, GameUtils.AvatarPlatformWindows);
                return new
                {
                    avatarInfo,
                    userId = player.UserId,
                    battlegroundsProfile,
                    twitchId = player.UserIdentity.TwitchId,
                    twitchUsername = player.UserIdentity.TwitchUsername
                };
            }
            throw new ArgumentException();
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("start")]
        public async Task<ActionResult<object>> Start()
        {
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
                        Players = matchHistory.Select(x => new BattlegroundsGameHistoryOutput(played, x.g, x.u)).ToArray(),

                    });
                }

                Dictionary<int, BattlegroundsGameHistoryOutput> summed = new Dictionary<int, BattlegroundsGameHistoryOutput>();

                for (int i = 0; i < matchHistoryContainer.Count; i++)
                {
                    var m = matchHistoryContainer[i];
                    foreach (var item in m.Players)
                    {
                        if (!summed.TryGetValue(item.UserId, out var history))
                        {
                            summed.Add(item.UserId, item);
                        }
                        else
                        {
                            history.Add(item);
                        }
                    }
                }


                return new TournamentInfo()
                {
                    MatchHistoryContainer = matchHistoryContainer,
                    MatchHistorySummary = summed.Values.ToArray(),
                    MapIndex = tournament.MapIndex,
                    IsComplete = tournament.Completed,
                    IsTournament = true
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
        [HttpPost("create")]
        public async Task<ActionResult<object>> Create([FromBody] BattlegroundsCreateGameRequest request)
        {
            try
            {

                var sender = this.GetCachedUser();
                if (sender != null)
                {
                    var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame == null)
                    {


                        activeGame = CreateGameInstance(request, sender);
                        if (request.Tournament)
                        {
                            var existingTournament = await _dbContext.Tournament.Where(x => x.UserId == sender.UserId && !x.Completed).FirstOrDefaultAsync();
                            if (existingTournament == null)
                            {
                                existingTournament = new Tournament();
                                existingTournament.UserId = sender.UserId;
                                existingTournament.TournamentId = Guid.NewGuid().ToString();
                                existingTournament.MapIndex = 0;
                                existingTournament.MapCount = request.TournamentMapCount.Value;
                                existingTournament.PointMethod = request.TournamentPointMethod.HasValue ? request.TournamentPointMethod.Value : 0;
                                _dbContext.Tournament.Add(existingTournament);
                            }
                            else
                            {
                                existingTournament.MapIndex++;
                                /*if (existingTournament.MapIndex == existingTournament.MapCount)
                                {
                                    //TODO: tournament completion stuff
                                    existingTournament.Completed = true;
                                }*/
                            }
                            activeGame.TournamentId = existingTournament.TournamentId;
                        }

                        _dbContext.GameInstance.Add(activeGame);
                        await _dbContext.SaveAsync();
                        return new
                        {
                            IsNewGame = true,
                            Players = new string[0],
                            ActiveGame = activeGame,
                            TournamentInfo = await GetTournamentInfo(activeGame)
                            /*GameId = activeGame.GameId,

							Payin = activeGame.Payin,
							Reward = activeGame.Reward
						*/
                        };
                    }
                    else
                    {
                        var playerIds = await _dbContext.BattlegroundsUser.Where(u => u.CurrentGameId == activeGame.GameId).Select(u => u.UserId).ToArrayAsync();
                        var twitchIds = await _dbContext.JoinUserModels().Where(u => playerIds.Contains(u.UserId)).Select(u => u.UserIdentity.TwitchId).ToArrayAsync();


                        if (twitchIds.Length > 0 || request.Tournament)
                        {

                            return new
                            {

                                IsNewGame = false,
                                Players = twitchIds,
                                ActiveGame = activeGame,
                                TournamentInfo = await GetTournamentInfo(activeGame)
                                /*
                                GameId = activeGame.GameId,
                                Payin = activeGame.Payin,
                                Reward = activeGame.Reward,
                                PlayerLimit = activeGame.pl*/
                            };
                        }
                        else
                        {
                            activeGame.Active = false;
                            var game = CreateGameInstance(request, sender);
                            _dbContext.GameInstance.Add(game);
                            await _dbContext.SaveAsync();
                            return new
                            {
                                IsNewGame = true,
                                Players = new string[0],
                                ActiveGame = game,
                                TournamentInfo = await GetTournamentInfo(activeGame)
                                /*GameId = activeGame.GameId,

                                Payin = activeGame.Payin,
                                Reward = activeGame.Reward
                            */
                            };
                        }
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

        GameInstance CreateGameInstance(BattlegroundsCreateGameRequest request, User sender)
        {
            var activeGame = new GameInstance();
            activeGame.Active = true;
            activeGame.HostId = sender.UserId;
            activeGame.Payin = request.Payin;
            activeGame.Reward = request.Reward;
            activeGame.Started = false;
            //     activeGame.HostDebitCornTxId = txid;
            activeGame.RewardMultiplier = 1;//request.RewardMultiplier;
            activeGame.PlayerLimit = request.MaxPlayerCount;
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
                    _dbContext.GameInstanceCornReward.Add(link);
                    return amount;
                }
            }
            return 0;
        }
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("processgame")]
        public async Task<ActionResult<object>> ProcessGame([FromBody] BattlegroundsProcessGameRequest request)
        {
            try
            {
                Tournament tournament = null;
                var sender = this.GetCachedUser();
                if (sender != null)
                {
                    var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
                    if (activeGame != null)
                    {
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
                        var users = await _dbContext.User.Where(p => playerIds.Contains(p.UserId)).AsNoTracking().ToDictionaryAsync(u => u.UserId, u => u);
                        //var existingUserIds = users.Keys.ToArray();

                        var allStats = _dbContext.BattlegroundsUser.Where(p => playerIds.Contains(p.UserId) && p.HostId == sender.UserId).ToDictionary(u => u.UserId, u => u);

                        var rewards = new decimal[0];
                        /*
						if (activeGame.Reward > 0)
						{
							rewards = SplitReward(activeGame.Reward, .5m, request.Players.Length);
						}*/
                        if (activeGame.Payin > 0)
                        {
                            rewards = new decimal[] {
                                //(activeGame.Payin*activeGame.RewardMultiplier)*users.Count
                                activeGame.Payin * playerIds.Length
                            };
                            //var tx = await _dbContext.CornTx.Where(u => u.CornTxId == activeGame.HostDebitCornTxId.Value)
                            //.Select(u => u.Amount).FirstOrDefaultAsync();
                            // if (tx != null)
                            {
                                //at the start of the game, the host was debited the max possible reward, refund whats left from the reward
                                /*var refund = tx.Value - rewards[0];
                                if (refund > 0)
                                {

                                    var rewardAmount = await SendfromBitcornhubTransaction(sender,
                                        activeGame.GameId,
                                        refund,
                                        "Battlegrounds host refund");

                                }*/
                            }
                        }
                        for (int i = 0; i < request.Players.Length; i++)
                        {
                            var userId = request.Players[i].UserId;
                            var history = new BattlegroundsGameHistory();
                            history.Add(request.Players[i]);
                            history.GameId = activeGame.GameId;
                            history.Placement = i;
                            history.UserId = request.Players[i].UserId;
                            if (tournament != null)
                            {
                                if (tournament.PointMethod == 0)
                                {
                                    history.Points = request.Players.Length - i;
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
                                if (i < rewards.Length && string.IsNullOrEmpty(activeGame.TournamentId))
                                {
                                    var reward = rewards[i];
                                    var rewardAmount = await SendfromBitcornhubTransaction(user, activeGame.GameId, reward, "Battlegrounds reward");
                                    rewards[i] = rewardAmount;

                                    if (allStats.TryGetValue(userId, out BattlegroundsUser stats))
                                    {
                                        if (i == 0)
                                        {
                                            stats.Wins++;
                                        }
                                        if (rewardAmount > 0)
                                        {
                                            stats.TotalCornRewards += reward;

                                        }
                                    }
                                }
                                else
                                {
                                    if (allStats.TryGetValue(userId, out BattlegroundsUser stats) && i == 0)
                                    {
                                        stats.Wins++;
                                    }

                                }
                            }
                        }

                        foreach (var player in allStats.Values)
                        {
                            player.GamesPlayed++;
                            player.Add(playerUpdates[player.UserId]);
                        }

                        if (allStats.Count > 0 || !activeGame.Active)
                        {
                            await _dbContext.SaveAsync();
                        }
                        if (!activeGame.Active && allStats.Count == 0)
                        {
                            await _dbContext.SaveAsync();

                        }

                        //if(allStats.Count == 0 && !activeGame.Active)
                        return new
                        {
                            rewards,
                            tournamentInfo = await GetTournamentInfo(activeGame)
                        };
                    }
                    return StatusCode((int)HttpStatusCode.Forbidden);


                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message + "::" + e.StackTrace);
                throw e;
            }
            throw new NotImplementedException();
        }
    }
}