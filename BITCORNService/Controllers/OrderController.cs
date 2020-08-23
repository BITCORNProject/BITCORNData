using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BITCORNService.Models;
using BITCORNService.Utils;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.LockUser;
using BITCORNService.Utils.Tx;
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
        /// reads stored order by id
        /// </summary>
        public async Task<ActionResult<object>> ReadOrder([FromBody]ReadOrderRequest orderRequest)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();

            var checkOrder = await GetOrder(orderRequest.OrderId);
            if (checkOrder == null) return StatusCode(404);

            var (order, client) = checkOrder.Value;
            if (client.ClientId != orderRequest.ClientId) return StatusCode(400);
            return new OrderOutput(order,client);

        }

        /// <summary>
        /// validates that order has been paid
        /// </summary>
        [Authorize(Policy = AuthScopes.CreateOrder)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost]
        public async Task<ActionResult> ValidateTransaction([FromBody]ValidateOrderTransactionRequest orderRequest)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();

            var checkOrder = await GetOrder(orderRequest.OrderId);
            if (checkOrder == null) return StatusCode(404);

            var (order, client) = checkOrder.Value;
            if (client.ClientId != orderRequest.ClientId) return StatusCode(400);

            if (orderRequest.TxId == order.TxId) return Ok();
            return StatusCode(400);
        }

        [Authorize(Policy = AuthScopes.CreateOrder)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost]
        /// <summary>
        /// called from client server to create an order tht the user will authorize
        /// </summary>
        public async Task<ActionResult<OrderOutput>> CreateOrder([FromBody]CreateOrderRequest orderRequest)
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
                    return new OrderOutput(order,client);
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
        public async Task<ActionResult<int>> AuthorizeOrder([FromBody]AuthorizeOrderRequest orderRequest)
        {
            var user = (this.GetCachedUser());
            if (user != null && !user.IsBanned)
            {
                var checkOrder = await GetOrder(orderRequest.OrderId);
                
                if (checkOrder == null) return StatusCode((int)HttpStatusCode.BadRequest);
                
                var (order, client) = checkOrder.Value;
               
                if (order.OrderState != 0) return StatusCode((int)HttpStatusCode.BadRequest);

                var recipientUser = await _dbContext.JoinUserModels()
                    .FirstOrDefaultAsync((u) => u.UserId == client.RecipientUser);

                if (recipientUser != null)
                {
                    var processInfo = await TxUtils.PrepareTransaction(user,
                        recipientUser,
                        order.Amount,
                        client.ClientId,
                        "app:order",
                        _dbContext);

                    var paymentSuccess = await processInfo.ExecuteTransaction(_dbContext);
                    if (paymentSuccess)
                    {
                        order.TxId = processInfo.Transactions[0].TxId;
                        order.OrderState = 1;

                        await _dbContext.SaveAsync();
                        return order.TxId.Value;
                    }

                }
            }

            return StatusCode((int)HttpStatusCode.BadRequest);
        }
        /// <summary>
        /// selects order, thirpartyclient
        /// </summary>
        public async Task<(Order, ThirdPartyClient)?> GetOrder(string orderId)
        {
            var output = await (from order in _dbContext.Order
                                join client in _dbContext.ThirdPartyClient on order.ClientId equals client.ClientId
                                select new
                                {
                                    order,
                                    client
                                }).FirstOrDefaultAsync((e) => e.order.OrderId == orderId);

            if (output != null)
                return (output.order, output.client);
            else return null;
        }

        public class OrderOutput
        {
            public string ClientName { get; set; }
            public string ClientId { get; set; }
            public string OrderName { get; set; }
            public string OrderDescription { get; set; }
            public decimal Amount { get; set; }
            public string OrderId { get; set; }
            public OrderOutput(Order order, ThirdPartyClient client)
            {
                ClientId = client.ClientId;
                ClientName = client.ClientName;
                OrderId = order.OrderId;
                OrderName = order.OrderName;
                OrderDescription = order.OrderDescription;
                Amount = order.Amount;
            }
        }

        public class AuthorizeOrderRequest
        {
            public string OrderId { get; set; }
            public string PlatformId { get; set; }
        }

        public class ReadOrderRequest
        {
            public string OrderId { get; set; }
            public string ClientId { get; set; }
        }
        public class ValidateOrderTransactionRequest
        {
            public int TxId { get; set; }
            public string OrderId { get; set; }
            public string ClientId { get; set; }
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
