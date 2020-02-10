using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public partial class Referrer
    {
        
        public int UserId { get; set; }
        [Key]
        public string ReferralId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }
    }
}
