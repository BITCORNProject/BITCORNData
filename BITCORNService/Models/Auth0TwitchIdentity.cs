using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class Auth0TwitchIdentity
    {
        public string TwitchId { get; set; }
        public string Auth0Id { get; set; }
    }
}
