using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BITCORNService.Utils.CommentUtils
{
    public static class CommentUtils
    {
        public static IQueryable<UserIdentity> FindUsersByName(BitcornContext dbContext, string username)
        {
            var srcUsername = username.ToLower();
            return dbContext.UserIdentity.Where((u) => u.Username.ToLower().Contains(username));

        }

        public static IQueryable<UserIdentity> FindUsersByName(BitcornContext dbContext, string[] usernames)
        {
            var srcUsernames = usernames.Select(x => x.ToLower()).ToArray();
            return dbContext.UserIdentity.Where((u) => srcUsernames.Contains(u.Username.ToLower()));

        }
        /*
        public static SocialComment CreateComment(int userId, string message, string rootId, string parent = null)
        {
            var comment = new SocialComment();
            comment.CommentId = CreateCommentId();
            comment.Message = message;
            comment.UserId = userId;
            if (!string.IsNullOrEmpty(rootId))
            {
                comment.RootId = rootId;
            }
            else
            {
                comment.RootId = comment.CommentId;
            }
            comment.ParentId = parent;
            comment.Timestamp = DateTime.Now;
            comment.IsListed = true;
            return comment;
        }

        public static string CreateCommentId()
        {
            var str = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return str.Substring(0, 22)
            .Replace("/", "_")
            .Replace("+", "-");
            //return str.Remove(str.Length-2);
        }

        public class SocialCommentUserIdentity
        {
            public SocialComment comment;
            public UserIdentity identity;
            public User user;
        }
        public static IQueryable<SocialCommentUserIdentity> JoinUser(BitcornContext dbContext)
        {
            return (from user in dbContext.User
                    join identity in dbContext.UserIdentity on user.UserId equals identity.UserId
                    join socialComment in dbContext.SocialComment on identity.UserId equals socialComment.UserId
                    select new SocialCommentUserIdentity()
                    {
                        user = user,
                        comment = socialComment,
                        identity = identity
                    }
                    );
          
        }

       
        public class SocialTagUserIdentity
        {
            public SocialTag Tag { get; set; }
            public UserIdentity Identity { get; set; }
        }


        public static IQueryable<SocialTagUserIdentity> JoinTags(BitcornContext dbContext)
        {
            return dbContext.SocialTag
                .Join(dbContext.UserIdentity,
                (tag) => tag.TagUserId,
                (identity) => identity.UserId,
                (tag, identity) => new SocialTagUserIdentity { Tag = tag, Identity = identity });
        }
        public static async Task<Dictionary<string, List<JoinedSocialTag>>> GetAllTags(BitcornContext dbContext, SocialComment[] rootComments)
        {
            var commentIds = rootComments.Select(x => x.CommentId).ToArray();
            var allTags = await JoinTags(dbContext)
                .Where(c => commentIds.Contains(c.Tag.CommentId)).ToArrayAsync();

            var tagDict = new Dictionary<string, List<JoinedSocialTag>>();

            for (int i = 0; i < rootComments.Length; i++)
            {
                tagDict.Add(rootComments[i].CommentId, new List<JoinedSocialTag>());
            }

            for (int i = 0; i < allTags.Length; i++)
            {
                if (!tagDict.TryGetValue(allTags[i].Tag.CommentId, out var list))
                {
                    list = new List<JoinedSocialTag>();
                    tagDict.Add(allTags[i].Tag.CommentId, list);
                }

                list.Add(new JoinedSocialTag(allTags[i].Tag, allTags[i].Identity));
            }

            return tagDict;
        }

        public static async Task<TagSelect[]> GetNotifications(BitcornContext dbContext, User user)
        {
            var ret = await GetNotifications(dbContext, new int[] { user.UserId });
           
            return ret;
        }

        public static async Task<TagSelect[]> GetNotifications(BitcornContext dbContext, int[] users)
        {
            var tags = await (from tag in dbContext.SocialTag
                              join socialComment in dbContext.SocialComment on tag.CommentId equals socialComment.CommentId

                              join identity in dbContext.UserIdentity on socialComment.UserId equals identity.UserId
                              select new
                              {
                                  Tag = tag,
                                  Identity = identity,
                                  Comment = socialComment,

                              }).Where(x => users.Contains(x.Tag.TagUserId) && x.Comment.IsListed && !x.Tag.Seen).OrderByDescending(x=>x.Comment.Timestamp).ToArrayAsync();

            return tags.Select((x) =>  new TagSelect()
            {
                RootId = x.Comment.RootId,
                CommentId = x.Comment.CommentId,
                Context = x.Comment.Context,
                ContextId = x.Comment.ContextId,
                Auth0Id = x.Identity.Auth0Id,
                NickName = x.Identity.Auth0Nickname,
                TaggedId = x.Tag.TagUserId
             
            }).ToArray();
        }

        public class TagSelect
        {
            public string CommentId { get; set; }
            public string Auth0Id { get; set; }
            public string NickName { get; set; }
            public int TaggedId { get; set; }
            public string RootId { get; set; }
            public string Context { get; set; }
            public string ContextId { get; set; }
        }

        internal static string GetId(string commentId)
        {
            if (string.IsNullOrEmpty(commentId)) return null;
            var commentIdSplit = commentId.Split(":");

            if (commentIdSplit.Length > 1)
            {
                if (commentIdSplit[0] == "explorer")
                {
                    //commentId = commentIdSplit[1];
                    var seed = commentIdSplit[1];
                    int seedNum = 0;
                    for (int i = 0; i < seed.Length; i++)
                    {
                        seedNum += (int)seed[i];
                    }

                    Random rnd = new Random(seedNum);
                    return RandomString(rnd, 22, false);
                }
            }
            return commentId;
        }

        public static string RandomString(Random random, int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);

            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26;

            for (var i = 0; i < size; i++)
            {
                var @char = (char)random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        public static string GetContext(string context)
        {
            return context.Split(":")[0];
        }

        public static async Task<SocialComment> GetRootComment(BitcornContext dbContext, SocialComment comment)
        {
            SocialComment lastComment = comment;
            var tempParentId = comment.ParentId;
            while (!string.IsNullOrEmpty(tempParentId))
            {
                var tempParent = await dbContext.SocialComment.Where(x => x.CommentId == tempParentId).FirstOrDefaultAsync();
                if (tempParent != null)
                {
                    lastComment = tempParent;
                    tempParentId = tempParent.ParentId;
                }
                else
                {
                    break;
                }
            }

            return lastComment;
        }
        */
    }
}
