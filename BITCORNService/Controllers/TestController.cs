using System;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpPost]
        public async Task<HttpStatusCode> Post([FromBody] dynamic data)
        {
            var value = data.test.Value;
            try
            {
                if (value == "test")
                {
                    return HttpStatusCode.OK;
                }
                else
                {
                    throw new Exception("Request body for this endpoint should contain {\"test\":\"test\"}");
                }
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(e);
                return HttpStatusCode.InternalServerError;
            }
        }

        [HttpGet]
        public async Task<HttpStatusCode> Get()
        {
            try
            {
                return HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(e);
                return HttpStatusCode.InternalServerError;
            }
        }
    }
}
