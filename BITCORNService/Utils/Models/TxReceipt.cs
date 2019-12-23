using BITCORNService.Models;

namespace BITCORNService.Utils.Models
{
    public class TxReceipt
    {
        public SelectableUser From { get; set; }
        public SelectableUser To { get; set; }
        public CornTx Tx { get; set; }
    }
}
