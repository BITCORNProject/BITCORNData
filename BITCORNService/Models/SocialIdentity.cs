using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class SocialIdentity
    {
        [Key]
        public string PlatformId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
