using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Games.Models;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
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
		[HttpGet("leaderboard/{orderby}")]
		public async Task<ActionResult<object>> Leaderboard([FromRoute] string orderby)
		{
			var properties = typeof(BattlegroundsUser)
				.GetProperties()
				.Select(p=>p.Name.ToLower())
				.ToArray();
			
			if (properties.Contains(orderby.ToLower()))
			{
				return await _dbContext.BattlegroundsUser.OrderByDescending(orderby).Join(_dbContext.UserIdentity,
					(stats)=>stats.UserId,
					(identity)=>identity.UserId,
					(s,i)=> new { 
						name = i.TwitchUsername,
						stats = s
					}).Take(100).ToArrayAsync();
				
			}
			return StatusCode((int)HttpStatusCode.BadRequest);
		}
		[ServiceFilter(typeof(LockUserAttribute))]
		[HttpPost("creategame")]
		public async Task<ActionResult> CreateGame([FromBody] BattlegroundsCreateGameRequest request)
		{
			var sender = this.GetCachedUser();
			if (sender != null && sender.IsAdmin())
			{
				var activeGame = await _dbContext.GameInstance.FirstOrDefaultAsync(g => g.HostId == sender.UserId && g.Active);
				if (activeGame == null)
				{

					activeGame = new GameInstance();
					activeGame.Active = true;
					activeGame.HostId = sender.UserId;
					activeGame.Payin = request.Payin;
					activeGame.Reward = request.Reward;
				}
				else
				{
					StatusCode((int)HttpStatusCode.Created);
				}
			}
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
					//map players to their ids
					var playerUpdates = request.Players.ToDictionary(u => u.UserId, u => u);
					var ids = playerUpdates.Keys.ToHashSet();
					//select registered users from database
					var users = await _dbContext.User.Where(p => ids.Contains(p.UserId)).Select(u => u.UserId).ToArrayAsync();
					//select stats from database (there can be user registered without battlegrounds profile)
					var allStats = _dbContext.BattlegroundsUser.Where(p => users.Contains(p.UserId)).ToDictionary(u => u.UserId, u => u);
					int newProfiles = 0;
					foreach (var user in users)
					{
						//no battlegrounds profile found for this user, create profile
						if (!allStats.ContainsKey(user))
						{
							var stats = new BattlegroundsUser();
							stats.UserId = user;
							_dbContext.BattlegroundsUser.Add(stats);
							allStats.Add(user, stats);
							newProfiles++;
						}
					}

					if (request.WinnerIndex != -1)
					{
						int winnerId = request.Players[request.WinnerIndex].UserId;

						if (allStats.TryGetValue(winnerId, out BattlegroundsUser stats))
						{
							stats.Wins++;
						}
					}

					foreach (var player in allStats.Values)
					{
						player.GamesPlayed++;
						player.Add(playerUpdates[player.UserId]);
					}

					if (allStats.Count > 0 || newProfiles > 0)
					{
						await _dbContext.SaveAsync();
					}
					return StatusCode(200);
				}
				return StatusCode(403);

			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
				throw e;
			}
		}
	}
}