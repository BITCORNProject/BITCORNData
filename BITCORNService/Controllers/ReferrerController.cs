﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
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
    public class ReferrerController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public ReferrerController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        // POST: api/Referrer
        [HttpPost]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<HttpStatusCode> Post([FromBody] ReferralUpload referralUpload)
        {
            var platformId = BitcornUtils.GetPlatformId(referralUpload.UserId);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null)
            {
                if (!_dbContext.Referrer.Any(r => r.UserId == user.UserId))
                {
                    try
                    {
                        if (referralUpload == null) throw new ArgumentNullException(nameof(referralUpload));

                        var referrer = new Referrer();
                        referrer.Amount = 10;
                        referrer.UserId = user.UserId;
                        referrer.Tier = 0;
                        referrer.YtdTotal = 0;
                        referrer.ETag = referralUpload.W9.ETag;
                        referrer.Key = referralUpload.W9.Key;
                        _dbContext.Referrer.Add(referrer);
                        await _dbContext.SaveAsync();
                        return HttpStatusCode.OK;
                    }
                    catch (Exception e)
                    {
                        await BITCORNLogger.LogError(_dbContext, e, JsonConvert.SerializeObject(referralUpload));
                        throw;
                    }
                }

                var referrer1 = await _dbContext.Referrer.FirstOrDefaultAsync(r => r.UserId == user.UserId);
                referrer1.ETag = referralUpload.W9.ETag;
                referrer1.Key = referralUpload.W9.Key;
                await _dbContext.SaveAsync();
                return HttpStatusCode.OK;
            }
            return HttpStatusCode.BadRequest;
        }
    }
}
