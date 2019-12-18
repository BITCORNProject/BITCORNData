using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils.Models
{
    public class RainBody
    {
        public string TwitchId { get; set; }

        public IEnumerable<TxUser> TxUsers { get; set; }
    }
}
