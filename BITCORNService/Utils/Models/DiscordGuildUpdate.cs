using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class DiscordGuildUpdate
    {
        public string GuildId { get; set; }
        public List<DiscordUserRoleUpdate> Roles { get; set; } = new List<DiscordUserRoleUpdate>();
    }
}
