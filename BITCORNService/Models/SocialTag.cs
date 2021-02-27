using System.ComponentModel.DataAnnotations;

namespace BITCORNService.Models
{
    public class SocialTag
    {
        [Key]
        public int SocialTagId { get; set; }
        public int TagUserId { get; set; }
        public string CommentId { get; set; }
        public bool Seen { get; set; }
        public int Idx { get; set; }

    }
    public class JoinedSocialTag 
    {
        
        public JoinedSocialTag(SocialTag tag, UserIdentity identity)
        {
            //this.SocialTagId = tag.SocialTagId;
            //this.TagUserId = tag.TagUserId;
            //this.CommentId = tag.CommentId;
            //this.Seen = tag.Seen;
            this.Idx = tag.Idx;
            this.Auth0Id = identity.Auth0Id;
            this.Nickname = identity.Auth0Nickname;
        }

        public string Auth0Id { get; set; }
        public string Nickname { get; set; }
        public int Idx { get; set; }
    }
}
