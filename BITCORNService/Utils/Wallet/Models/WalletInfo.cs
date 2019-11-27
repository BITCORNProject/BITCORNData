using System;

namespace BITCORNService.Utils.Wallet.Models
{
    /// <summary>
    /// Deserialized object parsed from getwalletinfo call
    /// </summary>
    [Serializable]
    public struct WalletInfo
    {
        public double walletversion { get; set; }

        public double balance { get; set; }

        public double txcount { get; set; }

        public double keypoololdest { get; set; }

        public double keypoolsize { get; set; }

        public double unlocked_until { get; set; }

    }
}

