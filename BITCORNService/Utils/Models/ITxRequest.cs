using BITCORNService.Models;
using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public interface ITxRequest
    {
        User FromUser { get; set; }
        decimal Amount { get; }
        string Platform { get; }
        string TxType { get; }
        IEnumerable<string> To { get; }
    }
}
