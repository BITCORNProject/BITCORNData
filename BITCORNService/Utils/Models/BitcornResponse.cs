using BITCORNService.Utils.Wallet.Models;
using System.Net;

namespace BITCORNService.Utils.Models
{
    public class BitcornResponse
    {
        public bool WalletAvailable { get; set; }
        public string WalletObject { get; set; }
        public bool UserError { get; set; }
        public WalletErrorCodes? ErrorCode { get; internal set; }
        public string DepositAddress { get; internal set; }
    }
}
