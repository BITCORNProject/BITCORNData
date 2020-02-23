using System;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class WalletDownload
    {
        [Key]
        public int DownloadId { get; set; }
        public int ReferralId { get; set; }
        public int? ReferralUserId { get; set; }
        public string ReferralCode { get; set; }
        public DateTime TimeStamp { get; set; }
        public string IncomingUrl { get; set; }
        public int? UserId { get; set; }
        public string Country { get; set; }
        public string IPAddress { get; set; }
        public string Platform { get; set; }
        public string WalletVersion { get; set; }

    }
}
