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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public UserController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost("getsubtiers")]
        public async Task<SubTierDiscord[]> GetSubTiers()
        {

            return await _dbContext.UserIdentity.
                Where(u => u.DiscordId != null).
                Join(_dbContext.User,
                identity => identity.UserId,
                us => us.UserId,
                (id, u) => new SubTierDiscord(id.DiscordId, u.SubTier)).
                ToArrayAsync();
        }
        // POST: api/User
        [HttpPost("{id}")]
        public async Task<FullUser> Post([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();

            return BitcornUtils.GetFullUser(user, user.UserIdentity, user.UserWallet, user.UserStat);
        }
        [HttpPost("ban/{id}")]
        public async Task<object> Ban([FromRoute] string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var primaryKey = -1;

            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                primaryKey = user.UserId;
                user.IsBanned = true;
                _dbContext.Update(user);

                await _dbContext.SaveAsync();
            }
            var users = await UserReflection.GetColumns(_dbContext, new string[] { "*" }, new[] { primaryKey });
            return users.First();
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
            //Update [user] 
//            set SubTier = $"{sub.Tier}"
//            inner join userIdentity
//            on [user].UserId = UserIdentity.UserId
//            where twitchid = $"sub.twitchId"
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

        [HttpPost]
        public async Task<HttpStatusCode> Post([FromBody] dynamic data)
        {
            //Update [user] 
//            set SubTier = $"{sub.Tier}"
//            inner join userIdentity
//            on [user].UserId = UserIdentity.UserId
//            where twitchid = $"sub.twitchId"
            try
            {
                var subData = data.ToString(Formatting.None);
                List<Sub> subs = JsonConvert.DeserializeObject<List<Sub>>(subData);

                var t1 = subs.Where(s => s.Tier == "1000").ToList();
                var t2 = subs.Where(s => s.Tier == "2000").ToList();
                var t3 = subs.Where(s => s.Tier == "3000").ToList();

                using (var db = new BitcornContext())
                {
                    await db.Database.ExecuteSqlCommandAsync("UPDATE users SET subtier = '0000'");
                }

                //tier 1 subs
                var sb = new StringBuilder();
                var prefix = "UPDATE [user] SET subtier = '1000'" +
                             "FROM [userIdentity] " +
                             "inner join [user]" +
                             "  on [useridentity].userid = [user].userid" +
                             " where [useridentity].twitchid in (";
                sb.Append(prefix);

                foreach (var sub in t1)
                {
                    sb.Append($"{sub.TwitchId}, ");
                }

                sb.Length -= 2;
                sb.Append(")");

                var query = sb.ToString().Substring(0, sb.ToString().Length - 4);
                using (var db = new BitcornContext())
                {
                    await db.Database.ExecuteSqlCommandAsync(query);
                }

                //tier 2 subs
                var sb2 = new StringBuilder();
                var prefix2 = "UPDATE [user] SET subtier = '2000'" +
                              "FROM [userIdentity] " +
                              "inner join [user]" +
                              "  on [useridentity].userid = [user].userid" +
                              " where [useridentity].twitchid in (";
                sb2.Append(prefix2);
                foreach (var sub in t2)
                {
                    sb2.Append($"{sub.TwitchId}, ");
                }

                sb2.Length -= 2;
                sb2.Append(")");

                var query2 = sb2.ToString().Substring(0, sb2.ToString().Length - 4);
                using (var db = new BitcornContext())
                {
                    await db.Database.ExecuteSqlCommandAsync(query2);
                }

                //tier 3 subs
                var sb3 = new StringBuilder();
                var prefix3 = "UPDATE [user] SET subtier = '3000'" + 
                              "FROM [userIdentity] " +
                              "inner join [user]" +
                              "  on [useridentity].userid = [user].userid" +
                              " where [useridentity].twitchid in (";
                sb3.Append(prefix3);
                foreach (var sub in t3)
                {
                    sb3.Append($"{sub.TwitchId}, ");
                }

                sb3.Length -= 2;
                sb3.Append(")");
                var query3 = sb3.ToString().Substring(0, sb3.ToString().Length - 4);
                using (var db = new BitcornContext())
                {
                    await db.Database.ExecuteSqlCommandAsync(query3);
                }


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