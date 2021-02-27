using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Models
{
    public class SocialComment
    {
		[Key]
		public string CommentId { get; set; }
		public string ParentId { get; set; }
		public string ContextId { get; set; }
		public string RootId { get; set; }
		public string Message { get; set; }

		public int Likes { get; set; }
		public int Dislikes { get; set; }
		public int TipCount { get; set; }

		public int UserId { get; set; }
		public DateTime Timestamp { get; set; }
        public string MediaId { get; set; }
        public bool IsListed { get; set; }
        public string Context { get; set; }
    }
	public class SocialCommentResponse : SocialComment
    {
		public string Auth0Id { get; set; }
		public string NickName { get; set; }
		public JoinedSocialTag[] Tags { get; set; }
        public SocialCommentResponse(SocialComment comment, UserIdentity user, IEnumerable<JoinedSocialTag> tags)
        {
			this.IsListed = comment.IsListed;
			this.CommentId = comment.CommentId;
			this.ParentId = comment.ParentId;
			this.Timestamp = comment.Timestamp;
			if (this.IsListed)
			{
				this.Tags = tags.ToArray();
				this.Message = comment.Message;
				this.Likes = comment.Likes;
				this.Dislikes = comment.Dislikes;
				this.TipCount = comment.TipCount;
				this.UserId = comment.UserId;
				
				this.MediaId = comment.MediaId;
				this.NickName = user.Auth0Nickname;
				this.Auth0Id = user.Auth0Id;

				this.Context = comment.Context;
				this.ContextId = comment.ContextId;
			}
			else
            {
				this.Tags = new JoinedSocialTag[0];
            }
        }
    }
}
