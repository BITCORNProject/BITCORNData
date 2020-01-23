using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Twitch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private IConfiguration _config;

        public UserController(IConfiguration config,BitcornContext dbContext)
        {
            _dbContext = dbContext;
            _config = config;
        }
        [HttpPost("updatesubs")]
        public async Task<HttpStatusCode> UpdateSubs()
        {
            try
            {
                var krak = new Kraken(_config, _dbContext);
                await krak.Nachos();
                return HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext,e);
                return HttpStatusCode.InternalServerError;
            }
        }
        // POST: api/User
        [HttpPost("{id}")]
        public async Task<ActionResult<FullUser>> Post([FromRoute] string id)
        {
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
        [HttpPost("ban")]
        public async Task<ActionResult<object>> Ban([FromBody] BanUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Sender)) throw new ArgumentNullException("Sender");

            if (string.IsNullOrWhiteSpace(request.BanUser)) throw new ArgumentNullException("BanUser");

            var senderPlatformId = BitcornUtils.GetPlatformId(request.Sender);
            var senderUser = await BitcornUtils.GetUserForPlatform(senderPlatformId, _dbContext).FirstOrDefaultAsync();
            if (senderUser!=null&&senderUser.Level == "5000")
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
        [HttpGet("{name}/[action]")]
        public bool Check(string name)
        {
            return _dbContext.User.Any(u => u.Username == name);
        }

        [HttpPut("[action]")]
        public async Task<bool> Update([FromBody] Auth0IdUsername auth0IdUsername)
        {
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
       
        [HttpPost]
        public async Task<HttpStatusCode> Post([FromBody] Sub[] subs)
        {
            try
            {
                var t1 = subs.Where(s => s.Tier == "1000").ToList();
                var t2 = subs.Where(s => s.Tier == "2000").ToList();
                var t3 = subs.Where(s => s.Tier == "3000").ToList();

                await _dbContext.Database.ExecuteSqlRawAsync("UPDATE [user] SET [subtier] = 0");
                if(t1.Count>0)
                    await _dbContext.Database.ExecuteSqlRawAsync(BitcornUtils.BuildSubtierUpdateString(t1,1));
                if(t2.Count>0)
                    await _dbContext.Database.ExecuteSqlRawAsync(BitcornUtils.BuildSubtierUpdateString(t2, 2));
                if (t3.Count > 0)
                    await _dbContext.Database.ExecuteSqlRawAsync(BitcornUtils.BuildSubtierUpdateString(t3, 3));
                return HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                //_logger.LogError(e, $"Failed to update subtiers {data}");
                return HttpStatusCode.InternalServerError;
            }
        }
    }
}