using Newtonsoft.Json;
using System;

namespace BITCORNService.Utils.Wallet.Models
{
    /// <summary>
    /// Deserialized Transaction info response returned by the gettransaction call
    /// </summary>
    [Serializable]
    public class BlockchainTX
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }
        [JsonProperty("confirmations")]
        public double Confirmations { get; set; }
        [JsonProperty("generated")]
        public bool Generated { get; set; }
        [JsonProperty("blockhash")]
        public string Blockhash { get; set; }
        [JsonProperty("blockindex")]
        public double blockindex { get; set; }
        [JsonProperty("blocktime")]
        public long Blocktime { get; set; }
        [JsonProperty("txid")]
        public string TxId { get; set; }
        [JsonProperty("walletconflicts")]
        public object[] WalletConflicts { get; set; }
        [JsonProperty("time")]
        public long Time { get; set; }
        [JsonProperty("timereceived")]
        public long TimeReceived { get; set; }
        [JsonProperty("details")]
        public BlockchainTxDetail[] Details { get; set; }
        [JsonProperty("hex")]
        public string Hex { get; set; }

    }
}

