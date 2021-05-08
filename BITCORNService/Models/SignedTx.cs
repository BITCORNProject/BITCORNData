using System;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class SignedTx
    {
        [Key]
        public string Id { get; set; }
        public int SenderUserId { get; set; }
        public int InCornTxId { get; set; }
        public int? OutCornTxId { get; set; }
        public int? ReceiverUserId { get; set; }
        public decimal Amount { get; set; }
        public string TxType { get; set; }
        public DateTime Timestamp { get; set; }

    }
}
