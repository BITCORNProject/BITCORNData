using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public OrderController(BitcornContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [Authorize(Policy = AuthScopes.CreateOrder)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost]
        /// <summary>
        /// called from client server to create an order tht the user will authorize
        /// </summary>
        public async Task<ActionResult<Order>> CreateOrder(CreateOrderRequest orderRequest)
        {
            try
            {
                if (this.GetCachedUser() != null)
                    throw new InvalidOperationException();

                var client = await _dbContext.ThirdPartyClient.FirstOrDefaultAsync((e) => e.ClientId == orderRequest.ClientId);
                if (client != null)
                {
                    var order = new Order();
                    order.ClientId = client.ClientId;
                    order.Amount = orderRequest.Amount;
                    order.OrderDescription = orderRequest.OrderDescription;
                    order.OrderName = orderRequest.OrderName;
                    order.OrderId = Guid.NewGuid().ToString();
                    order.OrderState = 0;

                    _dbContext.Order.Add(order);
                    await _dbContext.SaveAsync();
                    return order;
                }

                return StatusCode(400);
            }
            catch (Exception e)
            {
                await BITCORNLogger.LogError(_dbContext,e,JsonConvert.SerializeObject(orderRequest));
                return StatusCode(500);
            }
        }

        [Authorize(Policy = AuthScopes.AuthorizeOrder)]
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost]
        /// <summary>
        /// called from client server to create an order tht the user will authorize
        /// </summary>
        public async Task AuthorizeOrder(AuthorizeOrderRequest orderRequest)
        {
            var user = (this.GetCachedUser());
            if (user != null && !user.IsBanned)
            {

            }
        }

        public class AuthorizeOrderRequest
        {
            public string OrderId { get; set; }
            public string PlatformId { get; set; }
        }
        public class CreateOrderRequest
        {
            public decimal Amount { get; set; }

            public string OrderName { get; set; }

            public string OrderDescription { get; set; }
        
            public string ClientId { get; set; }
        }

        
    }
}
