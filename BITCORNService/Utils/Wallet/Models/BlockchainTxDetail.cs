using System;

namespace BITCORNService.Utils.Wallet.Models
{
    /// <summary>
    /// Deserialized Transaction info detail
    /// </summary>
    [Serializable]
    public struct BlockchainTxDetail
    {
        public string account { get; set; }
        public string address { get; set; }
        public string category { get; set; }
        public double amount { get; set; }
        public double vout { get; set; }
    }
}

