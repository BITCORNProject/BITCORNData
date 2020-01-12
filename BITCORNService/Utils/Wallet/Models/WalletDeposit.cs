using Newtonsoft.Json;

namespace BITCORNService.Utils.Wallet.Models
{
    public class WalletDeposit
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        [JsonProperty("address")]
        public string Address { get; set; }
        [JsonProperty("txid")]
        public string TxId { get; set; }
    }
}
