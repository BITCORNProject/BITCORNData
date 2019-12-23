using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public interface ITxRequest
    {
        string From { get; }
        decimal Amount { get; }
        string Platform { get; }
        IEnumerable<string> To { get; }
    }
}
