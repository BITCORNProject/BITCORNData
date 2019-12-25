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
        public int? SenderId { get; set; }
        public int ReceiverId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string BlockchainTxId { get; set; }
        public string TxGroupId { get; set; }

        public virtual ICollection<UnclaimedTx> UnclaimedTx { get; set; }
    }
}
