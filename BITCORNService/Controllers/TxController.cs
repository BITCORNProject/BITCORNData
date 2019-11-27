using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.LockUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.Controllers
{
    [LockUser]
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        [HttpPost("{rain}")]
        public void Rain([FromBody] string value)
        {

        }

        [HttpPost("{tipcorn}")]
        public void Tipcorn([FromBody] string value)
        {

        }

    }
}
