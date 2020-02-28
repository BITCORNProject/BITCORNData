using System;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
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
        private BitcornContext _dbContext;
        public TestController(IConfiguration configuration, BitcornContext dbContext)
        {
            _config = configuration;
            _dbContext = dbContext;
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
                var nachos = await ProbitApi.GetCornPrice();
                return HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                return HttpStatusCode.InternalServerError;
            }
        }
    }
}
