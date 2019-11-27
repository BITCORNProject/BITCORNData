using Newtonsoft.Json;
using System;

namespace BITCORNService.Utils.Wallet.Models
{
    /// <summary>
    /// Deserialized Transaction info detail
    /// </summary>
    [Serializable]
    public struct BlockchainTxDetail
    {
        [JsonProperty("account")]
        public string Account { get; set; }
        [JsonProperty("address")]
        public string Address { get; set; }
        [JsonProperty("category")]
        public string Category { get; set; }
        [JsonProperty("amount")]
        public double Amount { get; set; }
        [JsonProperty("vout")]
        public double Vout { get; set; }
    }
}

