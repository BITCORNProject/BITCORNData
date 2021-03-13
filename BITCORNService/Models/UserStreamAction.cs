using System;
using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class UserStreamAction
    {
        [Key]
        public int UserActionId { get; set; }
        
        public int RecipientUserId { get; set; }
      
        public int SenderUserId { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public bool Closed { get; set; }
        public int? TxId { get; set; }
        public DateTime? Timestamp { get; set; }
    }
    public class UserTts
    {
        [Key]
        public int UserId { get; set; }
        public decimal Pitch { get; set; }
        public decimal Rate { get; set; }
        public int Voice { get; set; }
        public int Uses { get; set; }
    }
}
