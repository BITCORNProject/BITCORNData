using System.Linq;
﻿using System;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BITCORNService.Utils.LockUser;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserIdentityController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public UserIdentityController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("{id}")]
        public async Task<UserIdentity> Auth0([FromRoute] string id)
        {
            var platformId =  BitcornUtils.GetPlatformId(id);
            return await BitcornUtils.GetUserForPlatform(platformId, _dbContext).Select(u => u.UserIdentity).FirstOrDefaultAsync();
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] RegistrationData registrationData)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            var platformId = BitcornUtils.GetPlatformId(registrationData.PlatformId);
            var userIdentity = await BitcornUtils.GetUserIdentityForPlatform(platformId, _dbContext);
            if (registrationData.Auth0Id == userIdentity.Auth0Id)
            {
                await BitcornUtils.DeleteIdForPlatform(userIdentity, platformId, _dbContext);
                return Ok();

            }
            throw new Exception("Auth0Id did not match the Auth0Id in the database for this user");
        }
    }
}