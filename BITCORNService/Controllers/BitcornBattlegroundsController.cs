using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
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
	public class BitcornBattlegroundsController : ControllerBase
	{
		BitcornGameContext _dbContext = null;
		public BitcornBattlegroundsController(BitcornGameContext dbContext)
		{
			_dbContext = dbContext;
		}
		[HttpGet("leaderboard/{orderby}")]
		public async Task<ActionResult<BattlegroundsUserStats[]>> Leaderboard([FromRoute] string orderby)
		{
			var properties = typeof(BattlegroundsUserStats)
				.GetProperties(BindingFlags.GetProperty|BindingFlags.Instance)
				.Select(p=>p.Name.ToLower());

			if (properties.Contains(orderby.ToLower()))
			{
				return await _dbContext.BattlegroundsGameStats
					.FromSqlRaw($"select * from {nameof(BattlegroundsUserStats)} order by {orderby}")
					.Take(100)
					.ToArrayAsync();
			}
			return StatusCode((int)HttpStatusCode.BadRequest);
		}

		[ServiceFilter(typeof(CacheUserAttribute))]
		[HttpPost("processgame")]
		public async Task<ActionResult> ProcessGame([FromBody] BitcornBattlegroundsProcessGameRequest request)
		{
			
			var user = this.GetCachedUser();
			if (user != null && user.IsAdmin())
			{
				int winnerId = request.Players[request.WinnerIndex].UserId;
				var playerUpdates = request.Players.ToDictionary(u=>u.UserId,u=>u);
				
				var players = await _dbContext.BattlegroundsGameStats.Where(p=>playerUpdates.ContainsKey(p.UserId)).ToArrayAsync();
				var winner = players.FirstOrDefault(p=>p.UserId==winnerId);
				winner.Wins++;
				foreach (var player in players)
				{
					player.GamesPlayed++;
					player.Add(playerUpdates[player.UserId]);
				}
				await _dbContext.SaveAsync();
				return StatusCode(200);
			}
			return StatusCode(403);
		}
	}
}