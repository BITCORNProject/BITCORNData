using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class Auth0DiscordIdentity
    {
        public string Auth0Id { get; set; }
        public string DiscordId { get; set; }
    }
}
