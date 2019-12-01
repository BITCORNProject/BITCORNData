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
