using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public string OrderId { get; set; }
        public decimal CornAmount { get; set; }
        public decimal UsdAmount { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string ClientItemId { get; set; }
    }
}
