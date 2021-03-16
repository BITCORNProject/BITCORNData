using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class Price
    {
        [Key]
        public int Id { get; set; }
        public string Symbol { get; set; }
        public decimal LatestPrice { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
