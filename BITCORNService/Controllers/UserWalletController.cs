using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserWalletController : ControllerBase
    {
        private readonly BitcornContext _dbContext;

        public UserWalletController(BitcornContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost("{id}")]
        public async Task<UserWallet> Wallet([FromRoute] string id)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            return await BitcornUtils.GetUserForPlatform(platformId, _dbContext).Select(u => u.UserWallet).FirstOrDefaultAsync();
        }
    }
}
