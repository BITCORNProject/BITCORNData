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
        IEnumerable<string> ITxRequest.To
        {
            get
            {
                yield return this.To;
            }
        }

    }
}
