using System;
using Newtonsoft.Json;

namespace BITCORNService.Utils.Wallet.Models
{
    /// <summary>
    /// Deserialized object parsed from getwalletinfo call
    /// </summary>
    [Serializable]
    public struct WalletInfo
    {
        [JsonProperty("walletversion")]
        public double WalletVersion { get; set; }
        [JsonProperty("balance")]
        public double Balance { get; set; }
        [JsonProperty("txcount")]
        public double TxCount { get; set; }
        [JsonProperty("keypoololdest")]
        public double KeypoolOldest { get; set; }
        [JsonProperty("keypoolsize")]
        public double KeypoolSize { get; set; }
        [JsonProperty("unlocked_until")]
        public double UnlockedUntil { get; set; }

    }
}

