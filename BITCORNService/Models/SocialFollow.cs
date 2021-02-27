using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class SocialFollow
    {
		[Key]
		public int SocialFollowId { get; set; }
		public int UserId { get; set; }
		public int FollowId { get; set; }
	}
}
