using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class CornPurchase
    {
        [Key]
        public int CornPurchaseId { get; set; }
        public decimal UsdAmount { get; set; }
        public decimal CornAmount { get; set; }
        public int UserId { get; set; }
        public int? CornTxId { get; set; }
        public string OrderId { get; set; }
        public string PaymentId { get; set; }
        public string Fingerprint { get; set; }
        public string ReceiptNumber { get; set; }
    }
}
