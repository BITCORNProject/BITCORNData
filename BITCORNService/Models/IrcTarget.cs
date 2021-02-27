using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class IrcTarget
    {
        [Key]
        public string TxGroupId { get; set; }
        public string IrcChannel { get; set; }
    }
}
