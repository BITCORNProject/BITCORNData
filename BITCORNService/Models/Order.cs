namespace BITCORNService.Models
{
    public class Order
    {
        public decimal Amount { get; set; }

        public string OrderName { get; set; }

        public string OrderDescription { get; set; }

        public string ClientId { get; set; }

        public string OrderId { get; set; }
        
        public int OrderState { get; set; }
    }
}
