using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class PlatformHeaders
    {
        public string Platform { get; set; }
        public string Id { get; set; }

        public override string ToString()
        {
            return $"{Platform}:{Id}";
        }

    }
}
