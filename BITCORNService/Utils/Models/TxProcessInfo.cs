using BITCORNService.Models;

namespace BITCORNService.Utils.Models
{
    public class TxProcessInfo
    {
        public TxReceipt[] Transactions { get; set; }
        public User From { get; set; }
    }
}
