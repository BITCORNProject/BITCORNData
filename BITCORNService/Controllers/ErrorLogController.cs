using System;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ErrorLogController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public ErrorLogController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        // POST: api/ErrorLog
        [HttpPost]
        public async Task<bool> Post([FromBody] ErrorLogs data)
        {
            try
            {
                _dbContext.ErrorLogs.Add(data);
                await _dbContext.SaveAsync();
                return true;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext,e,null);
                return false;
            }
        }
    }
}
