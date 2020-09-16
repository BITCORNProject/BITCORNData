using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
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
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using BITCORNService.Utils.Public;
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

                if (order.ReadBy != user.UserId)
                {
                    order.ReadBy = user.UserId;
                    await _dbContext.SaveAsync();
                }

                var orderItems = await _dbContext.OrderItem.Where(e=>e.OrderId==order.OrderId).ToArrayAsync();
                
                if(orderItems.Length>client.OrderMaxSize)
                {
                    return StatusCode((int)HttpStatusCode.NotAcceptable);
                }

                if(orderItems.Sum(e=>e.CornAmount)>client.OrderMaxCost)
                {
                    return StatusCode((int)HttpStatusCode.NotAcceptable);
                }


                var cornPrice = await ProbitApi.GetCornPriceAsync();
                var cornOrderSum = orderItems.Select(e => e.CornAmount).Sum();
                var cornCurrentSum = orderItems.Select(e => e.UsdAmount / cornPrice).Sum();
                var costDiff = Math.Abs(cornCurrentSum - cornOrderSum);

                if (costDiff > client.AcceptedCostDiff)
                {
                    foreach (var orderItem in orderItems)
                    {
                        orderItem.CornAmount = orderItem.UsdAmount / cornPrice;
                    }
                    await _dbContext.SaveAsync();
                    /*var cornAmount = usdAmount / cornPrice;
                        orderItem.CornAmount = cornAmount;*/
                }
                
                var username = user.Username;
                if(string.IsNullOrEmpty(username))
                {
                    username = user.UserIdentity.Auth0Nickname;
                }

                return new
                {
                    orderInfo = new OrderOutput(order, client, orderItems),
                    cornPrice,
                    totalCornPrice = orderItems.Select(e => e.CornAmount).Sum(),
                    totalUsdPrice = orderItems.Select(e => e.UsdAmount).Sum(),
                    userName = username,
                    balance = user.UserWallet.Balance
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
        [HttpPost("close")]
        public async Task<ActionResult<object>> CloseTransaction([FromBody] CloseOrderTransactionRequest orderRequest)
        {
            try
            {
                var appId = this.GetAppId(_configuration);
                if (string.IsNullOrEmpty(appId))
                    return StatusCode((int)HttpStatusCode.BadRequest);

                if (appId != orderRequest.ClientId)
                {
                    await BITCORNLogger.LogError(_dbContext,
                        new Exception("Unauthorized app request " + appId + " - " + orderRequest.ClientId),
                        JsonConvert.SerializeObject(orderRequest));

                    return StatusCode(401);
                }

                var checkOrder = await GetOrder(orderRequest.OrderId);
                if (checkOrder == null) return StatusCode(404);

                var (order, client) = checkOrder.Value;
                if (order.ClientId != orderRequest.ClientId) return StatusCode((int)HttpStatusCode.BadRequest);
                if (order.OrderState != 1) return StatusCode((int)HttpStatusCode.Gone);
                //if (client.ClientId != orderRequest.ClientId) return StatusCode(400);

                if (order.TxId != null && orderRequest.TxId == order.TxId)
                {
                    if (order.OrderState == 1)
                    {
                        order.OrderState = 2;
                        await _dbContext.SaveAsync();
                        return new 
                        { 
                            success=true
                        };
                    }
                    else
                    {
                        return HttpStatusCode.Gone;
                    }

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
        [HttpPost("create")]
        /// <summary>
        /// called from client server to create an order tht the user will authorize
        /// </summary>
        public async Task<ActionResult<OrderOutput>> CreateOrder([FromBody]CreateOrderRequest orderRequest)
        {
            try
            {
                var appId = this.GetAppId(_configuration);
                if (string.IsNullOrEmpty(appId))
                    return StatusCode((int)HttpStatusCode.BadRequest);

                if(appId!=orderRequest.ClientId)
                {
                    await BITCORNLogger.LogError(_dbContext,
                        new Exception("Unauthorized app request "+appId+" - "+orderRequest.ClientId),
                        JsonConvert.SerializeObject(orderRequest));

                    return StatusCode(401);
                }

                var client = await _dbContext.ThirdPartyClient.FirstOrDefaultAsync((e) => e.ClientId == appId);
                //await BITCORNLogger.LogError(_dbContext,new Exception(""),JsonConvert.SerializeObject(orderRequest));
                

                if (client != null)
                {
                    if (client.OrderMaxSize != null)
                    {
                        if (client.OrderMaxSize.Value > orderRequest.Items.Length)
                        {
                            return StatusCode((int)HttpStatusCode.NotAcceptable);
                        }
                    }
                    
                    var cornPrice = await ProbitApi.GetCornPriceAsync();
                    if (client.OrderMaxCost != null)
                    {
                       
                        var totalSum = orderRequest.Items.Select(i => i.AmountUsd / cornPrice).Sum();
                        if (totalSum > client.OrderMaxCost.Value)
                        {
                            return StatusCode((int)HttpStatusCode.NotAcceptable);
                        }
                    }
                    
                    var order = new Order();
                    order.ClientId = client.ClientId;
                    order.OrderId = Guid.NewGuid().ToString();
                    order.ClientOrderId = orderRequest.ClientOrderId.ToString();
                    OrderItem[] items = new OrderItem[orderRequest.Items.Length];
                    for (int i = 0; i < orderRequest.Items.Length; i++)
                    {
                        var orderItem = new OrderItem();
                        orderItem.Name = orderRequest.Items[i].Name;
                        orderItem.OrderId = order.OrderId;
                        orderItem.ClientItemId = orderRequest.Items[i].ItemId;
                        orderItem.Quantity = orderRequest.Items[i].Quantity;
                        if(orderItem.Quantity<1)
                        {
                            orderItem.Quantity = 1;
                        }
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
                    var orderItems = await _dbContext.OrderItem.Where(e => e.OrderId == order.OrderId).ToArrayAsync();

                    if (orderItems.Length > client.OrderMaxSize)
                    {
                        return StatusCode((int)HttpStatusCode.NotAcceptable);
                    }

                    if (orderItems.Sum(e => e.CornAmount) > client.OrderMaxCost)
                    {
                        return StatusCode((int)HttpStatusCode.NotAcceptable);
                    }

                    var recipientUser = await _dbContext.JoinUserModels()
                        .FirstOrDefaultAsync((u) => u.UserId == client.RecipientUser);
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
                                var jwt = CreateJwt(client,order,orderItems,cornOrderSum,processInfo.Transactions[0].TxId.Value);

                                order.TxId = processInfo.Transactions[0].TxId;
                                order.OrderState = 1;
                                order.CompletedAt = DateTime.Now;

                                await _dbContext.SaveAsync();
                                if (string.IsNullOrEmpty(client.Capture))
                                {
                                    return (new
                                    {
                                        jwt,
                                        txId = order.TxId.Value,
                                        amount = cornOrderSum
                                    });
                                }
                                else
                                {
                                    var restClient = new RestClient();
                                    var url = $"{client.Domain}/{client.Capture}";
                                    var redirectUrl = $"{client.Domain}/{client.Redirect}";

                                    var restRequest = new RestRequest(url, Method.POST);
                                    if (client.PostFormat == "application/x-www-form-urlencoded")
                                    {
                                        restRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                        restRequest.AddObject(new { jwt });
                                    }
                                    else
                                    {
                                        restRequest.AddJsonBody(new { jwt });
                                    }

                                    var restResponse =  restClient.Execute(restRequest);
                                
                                    return (new
                                    {
                                        redirect= redirectUrl,
                                        txId = order.TxId.Value,
                                        amount = cornOrderSum
                                    });
                                }
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

        private string CreateJwt(ThirdPartyClient client,Order order,OrderItem[] orderItems,decimal cornOrderSum,int txId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.Default.GetBytes(client.ValidationKey)), SecurityAlgorithms.HmacSha256Signature);
            List<Claim> claims = new List<Claim>();

            claims.Add(new Claim($"items", JsonConvert.SerializeObject(orderItems.Select(x => new {
                cornAmount = x.CornAmount,
                name = x.Name,
                usdAmount = x.UsdAmount,
                itemId = x.ClientItemId
            }).ToArray()
            )));

            claims.Add(new Claim("txInfo", JsonConvert.SerializeObject(new
            {
                orderId = order.OrderId,
                clientOrderId = order.ClientOrderId,
                totalAmount = cornOrderSum,
                txId = txId

            })));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims.ToArray()),
                Issuer = "https://bitcornfarms.com",

                SigningCredentials = creds
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
        
    }
}
