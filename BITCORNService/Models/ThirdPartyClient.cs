using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class ThirdPartyClient
    {
        [Key]
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public int? RecipientUser { get; set; }

        public decimal AcceptedCostDiff { get; set; }
        public string Domain { get; set; }
        public int? OrderMaxSize { get; set; }

        public decimal? OrderMaxCost { get; set; }

        public string ValidationKey { get; set; }

        public string Redirect { get; set; }

        public string Capture { get; set; }

        public string  PostFormat { get; set; }
    }
}
