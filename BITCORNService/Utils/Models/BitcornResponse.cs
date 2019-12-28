using System.Net;

namespace BITCORNService.Utils.Models
{
    public class BitcornResponse
    {
        public bool WalletAvailable { get; set; }
        public string WalletObject { get; set; }
        public bool UserError { get; set; }
    }
}
