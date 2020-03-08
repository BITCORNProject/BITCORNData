using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Games.Models
{
    public class ItemPrefab
    {
        [Key]
        public int Id { get; set; }
        public string AddressablePath { get; set; }
    }
}
