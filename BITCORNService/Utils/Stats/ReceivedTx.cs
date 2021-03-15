using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Stats
{
    public class ReceivedTx
    {
        public string Platform { get; set; }
        public DateTime? Timestamp { get; set; }
        public int CornTxId { get; set; }
        public decimal? Amount { get; set; }
        public string TxType { get; set; }
        public string TxGroupId { get; set; }
        public string TwitchUsername { get; set; }
        public string DiscordUsername { get; set; }
        public string TwitterUsername { get; set; }
        public string RedditUsername { get; set; }
        public string BlockchainTxId { get; set; }
        public string CornAddy { get; set; }
        public string Auth0Id { get; set; }
        public string Auth0Nickname { get; set; }

        public string Username { get; set; }
    }
}
