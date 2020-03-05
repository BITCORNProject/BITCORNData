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
	[Authorize]
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
					return await _dbContext.BattlegroundsUser.Where(u=>u.HostId==userId).OrderByDescending(orderby).Join(_dbContext.UserIdentity,
						(stats) => stats.UserId,
						(identity) => identity.UserId,
						(s, i) => new
						{
							name = i.TwitchUsername,
							stats = s
						}).Take(100).ToArrayAsync();

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

						//give corn to bitcornhub to hold for the duration of the game
						if (request.Reward > 0)
						{
							var tx = await TxUtils.SendToBitcornhub(sender, request.Reward, "BITCORNBattlegrounds", "Battlegrounds Host reward debit", _dbContext);
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
			
						_dbContext.GameInstance.Add(activeGame);
						await _dbContext.SaveAsync();
						return new { 
							Players = new string[0],
							GameId = activeGame.GameId
						};
					}
					else
					{
						var playerIds = await _dbContext.BattlegroundsUser.Where(u=>u.CurrentGameId==activeGame.GameId).Select(u=>u.UserId).ToArrayAsync();
						var twitchIds = await _dbContext.JoinUserModels().Where(u=>playerIds.Contains(u.UserId)).Select(u=>u.UserIdentity.TwitchId).ToArrayAsync();
						return new
						{
							Players = twitchIds,
							GameId = activeGame.GameId
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
		[ServiceFilter(typeof(CacheUserAttribute))]
		[HttpPost("processgame")]
		public async Task<ActionResult> ProcessGame([FromBody] BattlegroundsProcessGameRequest request)
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
						
						var rewards = SplitReward(activeGame.Reward, .5m, request.Players.Length);
						for (int i = 0; i < request.Players.Length; i++)
						{
							var userId = request.Players[i].UserId;
							var reward = rewards[i];

							if (users.TryGetValue(userId, out User user))
							{
								var receipt = await TxUtils.SendFromBitcornhubGetReceipt(user, reward, "BITCORNBattlegrounds", "Battlegrounds reward", _dbContext);
								if (receipt != null)
								{
									if (receipt.TxId != null)
									{
										var link = new GameInstanceCornReward();
										link.GameInstanceId = activeGame.GameId;
										link.TxId = receipt.TxId.Value;
										_dbContext.GameInstanceCornReward.Add(link);

										if (allStats.TryGetValue(userId, out BattlegroundsUser stats))
										{
											stats.TotalCornRewards += reward;
											if (i == 0)
											{
												stats.Wins++;

											}
										}

									}
								}
							}
						}
						
						foreach (var player in allStats.Values)
						{
							player.GamesPlayed++;
							player.Add(playerUpdates[player.UserId]);
						}

						if (allStats.Count > 0)
						{
							await _dbContext.SaveAsync();
						}
						return StatusCode(200);
					}
					return StatusCode((int)HttpStatusCode.Forbidden);


				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				throw e;
			}
			throw new NotImplementedException();
		}
	}
}