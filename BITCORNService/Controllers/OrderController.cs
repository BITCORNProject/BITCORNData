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
    //[Authorize]
    public class OrderController : ControllerBase
    {
        private readonly BitcornContext _dbContext;
        private readonly IConfiguration _configuration;
        public OrderController(BitcornContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }


        [Authorize(Policy = AuthScopes.AuthorizeOrder)]
        //[Authorize(Policy = AuthScopes.CreateOrder)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("read")]
        /// <summary>
        /// reads stored order by id
        /// </summary>
        public async Task<ActionResult<object>> ReadOrder([FromBody]ReadOrderRequest orderRequest)
        {
            try
            {
                
                var user = (this.GetCachedUser());
                if (this.GetUserMode() != null && this.GetUserMode() == 1) throw new NotImplementedException();
                if (user != null && user.IsBanned) return StatusCode((int)HttpStatusCode.Forbidden);
                
                var checkOrder = await GetOrder(orderRequest.OrderId);
                if (checkOrder == null) return StatusCode((int)HttpStatusCode.NotFound);
               
                var (order, client) = checkOrder.Value;
                if (client.ClientId != orderRequest.ClientId) return StatusCode(400);

                if (order.OrderState != 0) return StatusCode((int)HttpStatusCode.Gone);
                var items = await _dbContext.OrderItem.Where(e=>e.OrderId==order.OrderId).ToArrayAsync();
             
                var cornPrice = await ProbitApi.GetCornPriceAsync();
                return new
                {
                    orderInfo = new OrderOutput(order, client, items),
                    cornPrice,
                    totalCornPrice = items.Select(e => e.CornAmount).Sum(),
                    totalUsdPrice = items.Select(e => e.UsdAmount).Sum(),
                    userName = user.UserIdentity.Auth0Nickname
                };
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                throw e;
            }
        }

        /// <summary>
        /// validates that order has been paid
        /// </summary>
        [Authorize(Policy = AuthScopes.CreateOrder)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("validate")]
        public async Task<ActionResult> ValidateTransaction([FromBody] ValidateOrderTransactionRequest orderRequest)
        {
            try
            {
                if (this.GetCachedUser() != null)
                    throw new InvalidOperationException();

                var checkOrder = await GetOrder(orderRequest.OrderId);
                if (checkOrder == null) return StatusCode(404);

                var (order, client) = checkOrder.Value;
                if (order.OrderState != 1) return StatusCode((int)HttpStatusCode.Gone);
                //if (client.ClientId != orderRequest.ClientId) return StatusCode(400);

                if (order.TxId != null && orderRequest.TxId == order.TxId)
                {
                    order.OrderState = 2;
                    await _dbContext.SaveAsync();
                    return Ok();

                }
                return StatusCode(400);
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(_dbContext,e,JsonConvert.SerializeObject(orderRequest));
                return StatusCode(500);
            }
        }

        [Authorize(Policy = AuthScopes.CreateOrder)]
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("create")]
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
                    order.OrderId = Guid.NewGuid().ToString();
                    var cornPrice = await ProbitApi.GetCornPriceAsync();
                    OrderItem[] items = new OrderItem[orderRequest.Items.Length];
                    for (int i = 0; i < orderRequest.Items.Length; i++)
                    {
                        var orderItem = new OrderItem();
                        orderItem.Name = orderRequest.Items[i].Name;
                        orderItem.OrderId = order.OrderId;

                        var usdAmount = orderRequest.Items[i].AmountUsd;
                        var cornAmount = usdAmount / cornPrice;
                        orderItem.CornAmount = cornAmount;
                        orderItem.UsdAmount = orderRequest.Items[i].AmountUsd;
                        items[i] = orderItem;
                        _dbContext.OrderItem.Add(orderItem);
                    }

                    order.OrderState = 0;
                    order.CreatedAt = DateTime.Now;

                    _dbContext.Order.Add(order);
                    await _dbContext.SaveAsync();
                    return new OrderOutput(order, client, items);
                   
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
        [HttpPost("authorize")]
        /// <summary>
        /// called from client server to create an order tht the user will authorize
        /// </summary>
        public async Task<ActionResult<object>> AuthorizeOrder([FromBody]AuthorizeOrderRequest orderRequest)
        {
            try
            {
                var user = (this.GetCachedUser());
                if (this.GetUserMode() != null && this.GetUserMode() == 1) throw new NotImplementedException();

                if (user != null)
                {
                    if (user.IsBanned)
                    {
                        return StatusCode(403);
                    }

                    var checkOrder = await GetOrder(orderRequest.OrderId);

                    if (checkOrder == null) return StatusCode(404);
                    var (order, client) = checkOrder.Value;

                    if (order.OrderState != 0) return StatusCode((int)HttpStatusCode.Gone);
                    if (order.ClientId != orderRequest.ClientId) return StatusCode((int)HttpStatusCode.BadRequest);

                    var recipientUser = await _dbContext.JoinUserModels()
                        .FirstOrDefaultAsync((u) => u.UserId == client.RecipientUser);
                    var orderItems = await _dbContext.OrderItem.Where(e => e.OrderId == order.OrderId).ToArrayAsync();
                    var cornPrice = await ProbitApi.GetCornPriceAsync();
                    var cornOrderSum = orderItems.Select(e => e.CornAmount).Sum();
                    var cornCurrentSum = orderItems.Select(e => e.UsdAmount / cornPrice).Sum();
                    var costDiff = Math.Abs(cornCurrentSum - cornOrderSum);

                    if (costDiff <= client.AcceptedCostDiff)
                    {

                        if (recipientUser != null)
                        {
                            var processInfo = await TxUtils.PrepareTransaction(user,
                                recipientUser,
                                cornOrderSum,
                                client.ClientId,
                                "app:order",
                                _dbContext);

                            var paymentSuccess = await processInfo.ExecuteTransaction(_dbContext);
                            if (paymentSuccess)
                            {
                                order.TxId = processInfo.Transactions[0].TxId;
                                order.OrderState = 1;
                                order.CompletedAt = DateTime.Now;
                                await _dbContext.SaveAsync();
                                return (new
                                {
                                    txId = order.TxId.Value,
                                    amount = cornOrderSum
                                });
                            }
                            else
                            {
                                return new
                                {
                                    txId = -1
                                };
                            }

                        }
                    }
                    else
                    {
                        return StatusCode((int)HttpStatusCode.PaymentRequired);
                    }
                }

                return StatusCode((int)HttpStatusCode.BadRequest);
            }
            catch(Exception e)
            {
                await BITCORNLogger.LogError(_dbContext,e,JsonConvert.SerializeObject(orderRequest));
                return StatusCode(500);
            }
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
            public string OrderId { get; set; }
            public string Domain { get; set; }

            public object[] Items { get; set; }
            public OrderOutput(Order order, ThirdPartyClient client, OrderItem[] items)
            {
                ClientId = client.ClientId;
                ClientName = client.ClientName;
                OrderId = order.OrderId;
                Domain = client.Domain;
                this.Items = items.Select(e=>new {
                    cornAmount=e.CornAmount,
                    usdAmount=e.UsdAmount,
                    name=e.Name,
                  
                }).ToArray();
            }
        }

        public class AuthorizeOrderRequest
        {
            public string OrderId { get; set; }
            
            public string ClientId { get; set; }
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

        }
        public class CreateOrderRequest
        {
            public OrderItemRequest[] Items { get; set; }
        
            public string ClientId { get; set; }
        }

        public class OrderItemRequest
        {
            public string Name { get; set; }
            public decimal AmountUsd { get; set; }
        }

        
    }
}
