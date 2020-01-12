using BITCORNService.Utils.Wallet.Models;
using Newtonsoft.Json.Linq;

namespace BITCORNService.Models
{
    public class WalletDepositRequest
    {
        public int Index { get; set; }
        public string Block { get; set; }
        public WalletDeposit[] Payments { get; set; }
    }
    
}
