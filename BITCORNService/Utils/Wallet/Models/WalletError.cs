using System.Diagnostics;

namespace BITCORNService.Utils.Wallet.Models
{
    /// <summary>
    /// Deserialized error object returned by the wallet daemon
    /// </summary>
    public class WalletError
    {
        /// <summary>
        /// Wallet error code returned by the wallet daemon
        /// </summary>
        public WalletErrorCodes Code { get; set; }
        /// <summary>
        /// Wallet error message returned by the wallet daemon
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Stack trace to this error
        /// </summary>
        public StackTrace StackTrace { get; private set; }
        //TODO: add IError interface
        public WalletError()
        {
            this.StackTrace = new StackTrace();
        }
    }
}
