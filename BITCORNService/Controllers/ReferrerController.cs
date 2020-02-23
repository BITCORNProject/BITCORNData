using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<HttpStatusCode> Post([FromBody] ReferralUpload referralUpload)
        {
            try
            {
                if(referralUpload == null) throw new ArgumentNullException(nameof(referralUpload));

                var referrer = new Referrer();
                referrer.Amount = 10;
                referrer.UserId = referralUpload.UserId;
                referrer.Tier = 1;
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
    }
}
