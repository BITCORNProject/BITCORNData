using System;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Utils;
using BITCORNService.Utils.Twitch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private IConfiguration _config;
        public TestController(IConfiguration configuration)
        {
            _config = configuration;
        }

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
                return HttpStatusCode.InternalServerError;
            }
        }

        [HttpGet]
        public async Task<HttpStatusCode> Get()
        {
            try
            {
                var krak = new Kraken(_config);
                krak.Nachos();
                return HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                return HttpStatusCode.InternalServerError;
            }
        }
    }
}
