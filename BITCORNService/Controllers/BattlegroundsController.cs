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
				.Select(p=>p.Name.ToLower())
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
					battlegroundsProfile
				};
			}
			throw new ArgumentException();
		}
		[ServiceFilter(typeof(LockUserAttribute))]
		[HttpPost("create")]
		public async Task<ActionResult<object>> Create([FromBody] BattlegroundsCreateGameRequest request)
		{
			try
			{
				var sender = this.GetCachedUser();
				if (sender != null && sender.IsAdmin())
				{
					var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
					if (activeGame == null)
					{
						int? txid = null;
						var totalRewardAmount = (request.Payin * request.RewardMultiplier) * request.MaxPlayerCount;
						//give corn to bitcornhub to hold for the duration of the game
						if (totalRewardAmount > 100000000)
						{
							return StatusCode((int)HttpStatusCode.BadRequest);
						}
						if (totalRewardAmount > 0)
						{
							
							var tx = await TxUtils.SendToBitcornhub(sender, totalRewardAmount, "BITCORNBattlegrounds", "Battlegrounds Host reward debit", _dbContext);
							if (tx == null || tx.TxId == null)
							{
								return StatusCode((int)HttpStatusCode.PaymentRequired);
							}
							else
							{
								txid = tx.TxId;
							}
						}

						activeGame = new GameInstance();
						activeGame.Active = true;
						activeGame.HostId = sender.UserId;
						activeGame.Payin = request.Payin;
						activeGame.Reward = request.Reward;
						activeGame.HostDebitCornTxId = txid;
						activeGame.RewardMultiplier = request.RewardMultiplier;
						activeGame.PlayerLimit = request.MaxPlayerCount;
						_dbContext.GameInstance.Add(activeGame);
						await _dbContext.SaveAsync();
						return new { 
							IsNewGame = true,
							Players = new string[0],
							ActiveGame = activeGame
							/*GameId = activeGame.GameId,

							Payin = activeGame.Payin,
							Reward = activeGame.Reward
						*/
						};
					}
					else
					{
						var playerIds = await _dbContext.BattlegroundsUser.Where(u=>u.CurrentGameId==activeGame.GameId).Select(u=>u.UserId).ToArrayAsync();
						var twitchIds = await _dbContext.JoinUserModels().Where(u=>playerIds.Contains(u.UserId)).Select(u=>u.UserIdentity.TwitchId).ToArrayAsync();
						return new
						{

							IsNewGame = false,
							Players = twitchIds,
							ActiveGame = activeGame
							/*
							GameId = activeGame.GameId,
							Payin = activeGame.Payin,
							Reward = activeGame.Reward,
							PlayerLimit = activeGame.pl*/
						};
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
		public async Task<ActionResult<decimal[]>> ProcessGame([FromBody] BattlegroundsProcessGameRequest request)
		{
			try
			{
				var sender = this.GetCachedUser();
				if (sender != null && sender.IsAdmin())
				{
					var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
					if (activeGame != null)
					{
						activeGame.Active = false;
						
						//map players to their ids
						var playerUpdates = request.Players.ToDictionary(u => u.UserId, u => u);
						var ids = playerUpdates.Keys.ToHashSet();
						//select registered users from database
						var users = await _dbContext.User.Where(p => ids.Contains(p.UserId)).AsNoTracking().ToDictionaryAsync(u=>u.UserId,u=>u);
						var existingUserIds = users.Keys.ToArray();
				
						var allStats = _dbContext.BattlegroundsUser.Where(p => existingUserIds.Contains(p.UserId) && p.HostId == sender.UserId).ToDictionary(u => u.UserId, u => u);

						var rewards = new decimal[0];
						/*
						if (activeGame.Reward > 0)
						{
							rewards = SplitReward(activeGame.Reward, .5m, request.Players.Length);
						}*/
						if (activeGame.Payin > 0&&activeGame.HostDebitCornTxId!=null)
						{
							rewards = new decimal[] { 
								(activeGame.Payin*activeGame.RewardMultiplier)*users.Count
							};
							var tx = await _dbContext.CornTx.Where(u=>u.CornTxId==activeGame.HostDebitCornTxId.Value)
								.Select(u=>u.Amount).FirstOrDefaultAsync();
							if (tx != null)
							{
								//at the start of the game, the host was debited the max possible reward, refund whats left from the reward
								var refund = tx.Value - rewards[0];
								if (refund > 0)
								{
									
									var rewardAmount = await SendfromBitcornhubTransaction(sender,
										activeGame.GameId,
										refund,
										"Battlegrounds host refund");

								}
							}
						}
						for (int i = 0; i < request.Players.Length; i++)
						{
							var userId = request.Players[i].UserId;
						

							if (users.TryGetValue(userId, out User user))
							{
								if (i < rewards.Length)
								{
									var reward = rewards[i];
									var rewardAmount = await SendfromBitcornhubTransaction(user,activeGame.GameId,reward,"Battlegrounds reward");
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
									if (allStats.TryGetValue(userId, out BattlegroundsUser stats)&&i==0)
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

						if (allStats.Count > 0||!activeGame.Active)
						{
							await _dbContext.SaveAsync();
						}
						return rewards;
					}
					return StatusCode((int)HttpStatusCode.Forbidden);


				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message+"::"+e.StackTrace);
				throw e;
			}
			throw new NotImplementedException();
		}
	}
}