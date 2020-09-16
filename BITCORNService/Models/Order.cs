using System;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class Order
    {
        [Key]
        public string OrderId { get; set; }
        

        public string ClientId { get; set; }
        
        public int OrderState { get; set; }

        public int? TxId { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int ReadBy { get; set; }

        public string ClientOrderId { get; set; }
    }
}
