using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class SocialCommentInteraction
	{
		public string CommentId { get; set; }
		public int UserId { get; set; }
		public int Type { get; set; }

		[Key]
		public int SocialCommentInteractionId { get; set; }
	}
}
