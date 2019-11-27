using System;
using System.Collections.Generic;

namespace BITCORNService.Models
{
    public partial class CornTx
    {
        public CornTx()
        {
            UnclaimedTx = new HashSet<UnclaimedTx>();
        }

        public int CornTxId { get; set; }
        public string Platform { get; set; }
        public string TxType { get; set; }
        public decimal? Amount { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string BlockchainTxId { get; set; }

        public virtual ICollection<UnclaimedTx> UnclaimedTx { get; set; }
    }
}
