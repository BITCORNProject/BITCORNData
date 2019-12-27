using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class CornDeposit
    {
        [Key]
        public string TxId { get; set; }
        public int UserId { get; set; }
    }
}
