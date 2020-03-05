using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games.Models
{
    public class AvatarConfig
    {
        public int Id { get; set; }
        public string Catalog { get; set; }
        public string DefaultAvatar { get; set; }
        public string Platform { get; set; }
    }
}
