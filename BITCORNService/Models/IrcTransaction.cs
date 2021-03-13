using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class IrcTransaction
    {
        [Key]
        public string TxGroupId { get; set; }
        public string IrcChannel { get; set; }
        public string IrcMessage { get; set; }
    }
}
