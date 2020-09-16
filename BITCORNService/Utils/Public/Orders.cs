using BITCORNService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Public
{


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
            this.Items = items.Select(e => new {
                cornAmount = e.CornAmount,
                usdAmount = e.UsdAmount,
                name = e.Name,
                id = e.ClientItemId,
                quantity = e.Quantity
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
    public class CloseOrderTransactionRequest
    {
        public string ClientId { get; set; }
        public int TxId { get; set; }
        public string OrderId { get; set; }

    }
    public class CreateOrderRequest
    {
        public OrderItemRequest[] Items { get; set; }
        public string ClientOrderId { get; set; }
        public string ClientId { get; set; }
    }

    public class OrderItemRequest
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public string Name { get; set; }
        public decimal AmountUsd { get; set; }
    }

}
