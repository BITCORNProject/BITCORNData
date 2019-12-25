using BITCORNService.Models;
using System.Text.Json.Serialization;

namespace BITCORNService.Utils.Models
{
    public class TxReceipt
    {
        public SelectableUser From { get; set; }
        public SelectableUser To { get; set; }
        [JsonIgnore]
        public CornTx Tx { get; set; }
        public int TxId
        {
            get
            {
                if (Tx != null)
                {
                    return Tx.CornTxId;
                }
                else return -1;
            }
        }
    }
}
