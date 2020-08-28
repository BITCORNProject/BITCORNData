using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class ThirdPartyClient
    {
        [Key]
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public int? RecipientUser { get; set; }
    }
}
