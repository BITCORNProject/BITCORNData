using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Public;
using BITCORNServiceTests.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BITCORNServiceTests.ControllerTests
{
    public class OrderControllerTests
    {
        [Fact]
        public async Task TestCreateOrder()
        {
            await CreateOrder();   
        }
        [Fact]
        public async Task TestCloseOrder()
        {
            var clientId = "JyNM71Tg1b76GScmVpp31KQqFWfY5xbq";
            var orderOutput = await CreateOrder();
            var db = TestUtils.CreateDatabase();
            var orderController = new OrderController(db, TestUtils.GetConfig());
            var user = db.JoinUserModels().FirstOrDefault(u => u.UserId == 1722);
            var authResult = await ValidateAuth(user, orderController, clientId, orderOutput.OrderId);

            var result = JObject.Parse(JsonConvert.SerializeObject(authResult.Value)); ;

            int txId = (int)result["txId"];
            Assert.True(txId > 0);
            var closeResult = await orderController.CloseTransaction(new CloseOrderTransactionRequest() { 
                ClientId = clientId,
                OrderId = orderOutput.OrderId,
                TxId = txId
            });

            var r = JObject.Parse(JsonConvert.SerializeObject(closeResult.Value)); ;
            Assert.True((bool)r["success"]==true);
        }
        [Fact]
        public async Task TestCloseUnAuthorizedOrder()
        {
            var clientId = "JyNM71Tg1b76GScmVpp31KQqFWfY5xbq";
            var orderOutput = await CreateOrder();
            var db = TestUtils.CreateDatabase();
            var orderController = new OrderController(db, TestUtils.GetConfig());
            var user = db.JoinUserModels().FirstOrDefault(u => u.UserId == 1722);

            var closeResult = await orderController.CloseTransaction(new CloseOrderTransactionRequest()
            {
                ClientId = clientId,
                OrderId = orderOutput.OrderId,
                TxId = -1
            });

            Assert.Equal(410, (closeResult.Result as StatusCodeResult).StatusCode);
        }
        [Fact]
        public async Task TestAuthOrder()
        {
            var clientId = "JyNM71Tg1b76GScmVpp31KQqFWfY5xbq";
            var orderOutput = await CreateOrder();
            var db = TestUtils.CreateDatabase();
            var orderController = new OrderController(db, TestUtils.GetConfig());
            var user = db.JoinUserModels().FirstOrDefault(u => u.UserId == 1722);
            var authResult = await ValidateAuth(user,orderController,clientId,orderOutput.OrderId);

            var result = JObject.Parse(JsonConvert.SerializeObject(authResult.Value)); ;
            
            int txId = (int)result["txId"];
            Assert.True(txId > 0);
            var db2 = TestUtils.CreateDatabase();
            Assert.True(db2.CornTx.Any((e) => e.CornTxId == txId));
            var order = db2.Order.FirstOrDefault(e => e.OrderId == orderOutput.OrderId);
            Assert.True(order.OrderState == 1);
            Assert.True(order.TxId == txId);

            //
        }
        [Fact]
        public async Task TestAuthOrderNoFunds()
        {
            var clientId = "JyNM71Tg1b76GScmVpp31KQqFWfY5xbq";
            var db = TestUtils.CreateDatabase();
            var user = db.JoinUserModels().FirstOrDefault(u => u.UserId == 1722);

            
            var orderController = new OrderController(db, TestUtils.GetConfig());
            var orderOutput = await CreateOrder(10000);

            var authResult = await ValidateAuth(user, orderController, clientId, orderOutput.OrderId);
            
            var result = JObject.Parse(JsonConvert.SerializeObject(authResult.Value)); ;

            int txId = (int)result["txId"];
            Assert.True(txId == -1);
            
            //
        }

        async Task<ActionResult<object>> ValidateAuth(User user, OrderController orderController, string clientId, string orderId)
        {
            var context = orderController.ControllerContext.HttpContext = new DefaultHttpContext();
            context.Items.Add("user", user);
            var authResult = await orderController.AuthorizeOrder(new AuthorizeOrderRequest()
            {
                ClientId = clientId,
                OrderId = orderId
            });
            return authResult;

        }
        async Task<OrderOutput> CreateOrder(decimal amountUsd = 10)
        {
            var itemRequest = new OrderItemRequest[] {
                        new OrderItemRequest() {
                            AmountUsd = amountUsd,
                            ItemId = "itemid",
                            Name = "name",
                            Quantity = 1
                        }
                    };
            var clientOrderId = "order-id-123";
            var clientId = "JyNM71Tg1b76GScmVpp31KQqFWfY5xbq";
            var db = TestUtils.CreateDatabase();
            {

                var orderController = new OrderController(db, TestUtils.GetConfig());
                var order = await orderController.CreateOrder(new CreateOrderRequest()
                {
                    ClientId = clientId,
                    ClientOrderId = clientOrderId,
                    Items = itemRequest
                });


                var db2 = TestUtils.CreateDatabase();
                Assert.NotNull(order.Value);
                //var statusCode = (order.Result as StatusCodeResult).StatusCode;
                //Assert.Equal(200,statusCode);
                var srcOrder = db2.Order.FirstOrDefault((e) => e.OrderId == order.Value.OrderId);
                Assert.Equal(srcOrder.ClientOrderId, clientOrderId);
                var orderItems = db2.OrderItem.Where(e => e.OrderId == srcOrder.OrderId).ToArray();
                Assert.Equal(orderItems.Length, itemRequest.Length);
                for (int i = 0; i < orderItems.Length; i++)
                {
                    Assert.Equal(orderItems[i].UsdAmount, itemRequest[i].AmountUsd);

                    Assert.Equal(orderItems[i].Name, itemRequest[i].Name);
                    Assert.Equal(orderItems[i].Quantity, itemRequest[i].Quantity);
                    Assert.Equal(orderItems[i].ClientItemId, itemRequest[i].ItemId);

                }
                Assert.NotNull(srcOrder);
                Assert.Equal(0, srcOrder.OrderState);
                return order.Value;
            }
        }

    }
}
