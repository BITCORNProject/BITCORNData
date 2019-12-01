using Newtonsoft.Json.Linq;

namespace BITCORNService.Models
{
    public class WalletDepositRequest
    {
        public int Index { get; set; }
        public string Block { get; set; }
        public JArray Payments { get; set; }
    }
}
