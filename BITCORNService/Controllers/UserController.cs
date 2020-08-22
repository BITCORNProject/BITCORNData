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
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
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
        // POST: api/User   
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("{id}")]
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
        
        [HttpGet("userid/{id}")]
        public async Task<ActionResult<int>> UserId(string id)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId,_dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                return user.UserId;
            }
            return StatusCode(404);
        }

        [HttpGet("transactions/{id}/{offset}/{amount}/{txTypes}")]
        public async Task<ActionResult<object>> Transactions(string id,int offset,int amount,string txTypes= null)
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
                return BitcornUtils.GetFullUserAndReferer(user, user.UserIdentity, user.UserWallet, user.UserStat, user.UserReferral, referral);
            }
            else
            {
                return StatusCode(404);
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("me")]
        public ActionResult<FullUser> Me()
        {
            User user = null;
            if ((user = this.GetCachedUser()) == null)
                return StatusCode(404);
            return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
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
            if (senderUser!=null && senderUser.IsAdmin())
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
        public async Task<bool> Check(string name)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            return await _dbContext.User.AnyAsync(u => u.Username == name);
        }
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