using System;
using System.Collections.Generic;

namespace BITCORNService.Models
{
    public partial class UnclaimedTx
    {
        public int Id { get; set; }
        public DateTime? Expiration { get; set; }
        public int? SenderUserId { get; set; }
        public int? CornTxId { get; set; }
        public bool? Claimed { get; set; }
        public int? ReceiverUserId { get; set; }
        public string Platform { get; set; }
        public decimal? Amount { get; set; }
        public bool? Refunded { get; set; }
        public string TxType { get; set; }
        public DateTime? Timestamp { get; set; }

        public virtual CornTx CornTx { get; set; }
        public virtual User ReceiverUser { get; set; }
        public virtual User SenderUser { get; set; }
    }
}
