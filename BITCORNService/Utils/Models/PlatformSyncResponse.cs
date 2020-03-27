using BITCORNService.Models;
using System;

namespace BITCORNService.Utils.Models
{
    public class PlatformSyncResponse
    {
        public FullUser User { get; set; }
        public bool IsMigration { get; set; }
        public DateTime? SocialCreationTime { get; set; }
    }
}
