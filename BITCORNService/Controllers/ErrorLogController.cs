using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.Controllers
{
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
        public async Task Post([FromBody] ErrorLogs errorLogs)
        {
            _dbContext.ErrorLogs.Add(errorLogs);
            await _dbContext.SaveAsync();
        }
    }
}
