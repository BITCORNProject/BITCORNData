using BITCORNService.Models;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class TipRequest : ITxRequest
    {
        public string From { get; set; }

        public string To { get; set; }

        public decimal Amount { get; set; }

        public string Platform { get; set; }

        public string[] Columns { get; set; }
        public string IrcMessage { get; set; }

        string ITxRequest.TxType => GetTxType();
        protected virtual string GetTxType()
        {
            return "$tipcorn";
        }
        IEnumerable<string> ITxRequest.To
        {
            get
            {
                yield return this.To;
            }
        }

        [JsonIgnore]
        public User FromUser { get; set; }
        public string IrcTarget { get; set; }


    }

    public class ChannelPointsRedemptionRequest : TipRequest
    {
        public int ChannelPointAmount { get; set; }
        protected override string GetTxType()
        {
            return "channel-points";
        }
    }

    public class ChannelSubRequest : TipRequest
    {
        public  string SubTier { get; set; }
        protected override string GetTxType()
        {
            return "sub-event";
        }
    }

    public class BitDonationRequest : TipRequest
    {
        public decimal BitAmount { get; set; }
        protected override string GetTxType()
        {
            return "bit-donation";
        }
    }

}
