using BITCORNService.Models;

namespace BITCORNService.Utils.Models
{
    public class PlatformSyncResponse
    {
        public FullUser User { get; set; }
        public bool IsMigration { get; set; }
    }
}
