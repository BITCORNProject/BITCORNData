using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class DiscordUserRoleUpdate
    {
        public List<string> Users { get; set; } = new List<string>();
        public string RoleId { get; set; }

    }
}
