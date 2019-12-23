using BITCORNService.Models;

namespace BITCORNService.Utils.Models
{
    public class TxReceipt
    {
        public User From { get; set; }
        public User To { get; set; }
        public CornTx Tx { get; set; }
    }
}
