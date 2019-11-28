using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BanUserController : ControllerBase
    {
        // POST: api/BanUser
        [HttpPost]
        public void Post([FromBody] string value)
        {
            //sender id, reciever twitchId
            
            //banresult code 
        }
    }
}
