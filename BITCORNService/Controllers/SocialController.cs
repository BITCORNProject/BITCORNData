using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BITCORNService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialController : ControllerBase
    {
        /*
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/social/follows/{followId}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> IsFollowing([FromRoute] string id, [FromRoute] string followId)
        {

            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            var followPlatformId = BitcornUtils.GetPlatformId(followId);
            var followUser = await BitcornUtils.GetUserForPlatform(followPlatformId, _dbContext).FirstOrDefaultAsync();

            if (user != null && !user.IsBanned)
            {
                var isFollowing = await _dbContext.SocialFollow.AnyAsync((f) => f.UserId == user.UserId && f.FollowId == followUser.UserId);
                return new
                {
                    IsFollowing = isFollowing
                };

            }

            return StatusCode(404);
        }
        */
        /*
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/social/follow/{followId}/{doAction}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> Follow([FromRoute] string id, [FromRoute] string followId, [FromRoute] string doAction)
        {

            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            var followPlatformId = BitcornUtils.GetPlatformId(followId);
            var followUser = await BitcornUtils.GetUserForPlatform(followPlatformId, _dbContext).FirstOrDefaultAsync();

            if (user != null && followUser != null && !user.IsBanned)
            {
                var follow = await _dbContext.SocialFollow.FirstOrDefaultAsync((f) => f.UserId == user.UserId && f.FollowId == followUser.UserId);

                if (doAction == "unfollow")
                {
                    if (follow != null)
                    {
                        _dbContext.Remove(follow);
                        await _dbContext.SaveAsync();
                    }

                    return new
                    {
                        IsFollowing = false
                    };
                }
                else if (doAction == "follow")
                {
                    if (follow == null)
                    {
                        follow = new SocialFollow();
                        follow.FollowId = followUser.UserId;
                        follow.UserId = user.UserId;
                        _dbContext.SocialFollow.Add(follow);
                        await _dbContext.SaveAsync();
                    }

                    return new
                    {
                        IsFollowing = true
                    };

                    //
                }


            }

            return StatusCode(404);

        }


        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/social/comment")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<SocialCommentResponse>> CreateComment([FromRoute] string id, [FromBody] PostCommentBody body)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            try
            {
                if (body.Message.Length > 250) return StatusCode(404);

                var platformId = BitcornUtils.GetPlatformId(id);
                var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null && !user.IsBanned)
                {
                    if (string.IsNullOrEmpty(body.ParentId))
                        body.ParentId = null;
                    if (string.IsNullOrEmpty(body.MediaId))
                        body.MediaId = null;

                    string parentCommentId = null;
                    string rootCommentId = null;
                    string commentContext = null;
                    string commentContextId = null;
                    //bool isContextComment = false;
                    if (!string.IsNullOrEmpty(body.ParentId))
                    {
                        var parentComment = await _dbContext.SocialComment.FirstOrDefaultAsync(x => x.CommentId == CommentUtils.GetId(body.ParentId));
                        if (parentComment != null)
                        {
                            commentContext = parentComment.Context;
                            commentContextId = parentComment.ContextId;
                            parentCommentId = parentComment.CommentId;
                            var rootComment = await CommentUtils.GetRootComment(_dbContext, parentComment);
                            if (rootComment != null)
                            {
                                rootCommentId = rootComment.CommentId;
                            }
                        }

                    }
                    else
                    {
                        commentContext = body.Context;
                        commentContextId = body.ContextId;
                    }
                   
                    var removeTagsSentFromClient = new Regex(string.Format("\\{0}.*?\\{1}", "<tag:", ">"));

                    var rawMessage = removeTagsSentFromClient.Replace(body.Message, string.Empty);
                    //CommentUtils.GetId(body.ParentId)
                    var comment = CommentUtils.CreateComment(user.UserId, rawMessage, rootCommentId, parentCommentId);
                    comment.Context = commentContext;
                    comment.ContextId = commentContextId;
                  
                    comment.MediaId = body.MediaId;

                    var messageBuilder = new StringBuilder(rawMessage.ToLower());
                    var splitMsg = body.Message.ToLower().Split(" ");

                    var tags = splitMsg.Where(x => x.Length > 0 && x[0] == '@').Select(x => x.Remove(0, 1)).ToArray();
                    var srcUsers = await CommentUtils.FindUsersByName(_dbContext, tags).Select((u) => new
                    {
                        UserId = u.UserId,
                        NickName = u.Auth0Nickname,
                        Auth0Id = u.Auth0Id,
                        Identity = u
                    }).ToDictionaryAsync(u => u.NickName.ToLower(), u => u);

                    List<JoinedSocialTag> newTags = new List<JoinedSocialTag>();
                    int tagIndex = 0;
                    for (int i = 0; i < tags.Length; i++)
                    {
                        var srcTag = tags[i];
                        if (srcUsers.TryGetValue(srcTag.ToLower(), out var obj))
                        {
                            var username = $"@{obj.NickName.ToLower()}";

                            if (!messageBuilder.ToString().Contains(username)) continue;

                            var tag = new SocialTag();
                            tag.CommentId = comment.CommentId;
                            tag.TagUserId = obj.UserId;
                            tag.Seen = false;
                            tag.Idx = tagIndex;

                            var insertString = $"<tag:{tagIndex}>";
                            messageBuilder.Replace(username, insertString);
                            tagIndex++;
                            newTags.Add(new JoinedSocialTag(tag, obj.Identity));
                            _dbContext.SocialTag.Add(tag);
                        }
                    }


                    comment.Message = messageBuilder.ToString();
                    _dbContext.SocialComment.Add(comment);
                    await _dbContext.SaveAsync();

                    return new SocialCommentResponse(comment, user.UserIdentity, newTags);
                }

                return StatusCode(404);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/social/comment/{commentId}/interact/{type}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<SocialCommentResponse>> CommentInteraction([FromRoute] string id, [FromRoute] string commentId, [FromRoute] string type, [FromQuery] string reader)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                commentId = CommentUtils.GetId(commentId);
                var comment = await _dbContext.SocialComment.Where(c => c.CommentId == commentId).FirstOrDefaultAsync();

                if (comment != null)
                {
                    if (comment.UserId == TxUtils.BitcornHubPK) return StatusCode(404);

                    int? previousInteractionType = null;
                    SocialCommentInteraction previousInteraction = null;
                    SocialCommentInteraction interaction = null;
                    if (type != "tip")
                    {
                        previousInteraction =
                            await _dbContext.SocialCommentInteraction
                            .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId
                                && c.UserId == user.UserId
                                && c.Type != 2);
                    }

                    if (previousInteraction != null)
                    {
                        interaction = previousInteraction;
                        previousInteractionType = interaction.Type;
                    }
                    else
                    {
                        if (type != "undo")
                        {
                            interaction = new SocialCommentInteraction();
                            interaction.UserId = user.UserId;
                            interaction.CommentId = comment.CommentId;
                            _dbContext.SocialCommentInteraction.Add(interaction);
                            //await _dbContext.SaveAsync();
                        }
                    }


                    if (type == "like")
                    {
                        interaction.Type = 1;
                        if (previousInteractionType == -1)
                        {
                            comment.Dislikes--;
                        }

                        if (previousInteractionType != 1)
                        {
                            comment.Likes++;
                        }

                    }

                    if (type == "dislike")
                    {
                        interaction.Type = -1;
                        if (previousInteractionType == 1)
                        {
                            comment.Likes--;

                        }

                        if (previousInteractionType != -1)
                        {
                            comment.Dislikes++;
                        }
                    }


                    if (type == "tip")
                    {
                        interaction.Type = 2;
                        comment.TipCount++;
                    }

                    if (type == "undo")
                    {

                        if (previousInteractionType == 1)
                        {
                            comment.Likes--;
                        }

                        if (previousInteractionType == -1)
                        {
                            comment.Dislikes--;
                        }

                        interaction.Type = -10;

                    }

                    await _dbContext.SaveAsync();
                    var tags = await CommentUtils.JoinTags(_dbContext)
                        .Where(c => c.Tag.CommentId == comment.CommentId).ToArrayAsync();
                    return new SocialCommentResponse(comment,
                        await _dbContext.UserIdentity.FirstOrDefaultAsync(u => u.UserId == comment.UserId),
                        tags.Select(x => new JoinedSocialTag(x.Tag, x.Identity)));
                }
            }

            return StatusCode(404);
        }





        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/social/comments")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetComments([FromRoute] string id, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

            var platformId = BitcornUtils.GetPlatformId(id);
            var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
            if (user != null && !user.IsBanned)
            {
                var rootComments = await _dbContext.SocialComment.Where(c => c.UserId == user.UserId && c.ParentId == null && c.IsListed)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(10)
                    .ToArrayAsync();
                var tagDict = await CommentUtils.GetAllTags(_dbContext, rootComments);
                return rootComments.Select(x => new SocialCommentResponse(x, user.UserIdentity, tagDict[x.CommentId])).ToArray();
               
            }

            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("social/commentbyid/{commentId}")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetCommentById([FromRoute] string commentId)
        {

            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            commentId = CommentUtils.GetId(commentId);

            var comments = await CommentUtils.JoinUser(_dbContext).Where((x) =>
                x.comment.CommentId == commentId && x.comment.IsListed
            ).ToArrayAsync();

            var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());
            return comments.Select((c) => new SocialCommentResponse(c.comment, c.identity, tagDict[c.comment.CommentId])).ToArray();



            return StatusCode(404);
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("social/comments/context/{contextId}")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetCommentsByContext([FromRoute] string contextId, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();


            var comments = await CommentUtils.JoinUser(_dbContext)
                .Where(c => c.comment.ContextId == contextId && c.comment.ParentId == null && c.comment.IsListed)
                  .OrderByDescending(x => x.comment.Timestamp)
                  .Take(10)
                  .ToArrayAsync();
            //var tagDict = await CommentUtils.GetAllTags(_dbContext, rootComments);
            var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());
            return comments.Select((c) => new SocialCommentResponse(c.comment, c.identity, tagDict[c.comment.CommentId])).ToArray();

            //return rootComments.Select(x => new SocialCommentResponse(x, user.UserIdentity, tagDict[x.CommentId])).ToArray();

        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("{id}/social/feed")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetFeed([FromRoute] string id)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            try
            {
                var platformId = BitcornUtils.GetPlatformId(id);
                var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                if (user != null && !user.IsBanned)
                {
                    var following = await _dbContext.SocialFollow.Where(u => u.UserId == user.UserId).ToArrayAsync();
                    var followingUserIds = following.Select((f) => f.FollowId).ToArray();

                    var comments = await CommentUtils.JoinUser(_dbContext)
                        .Where((x) => x.comment.IsListed && followingUserIds.Contains(x.comment.UserId) && x.comment.ParentId == null && !x.user.IsBanned && x.comment.Context == null)

                        .OrderByDescending(x => x.comment.Timestamp)
                            .Take(10)
                            .ToArrayAsync();

                    var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());
                    return comments.Select((c) => new SocialCommentResponse(c.comment, c.identity, tagDict[c.comment.CommentId])).ToArray();

                }

                return StatusCode(404);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpGet("social/comment/{commentId}/subcomments")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<SocialCommentResponse[]>> GetSubComments([FromRoute] string commentId, [FromQuery] string reader)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            commentId = CommentUtils.GetId(commentId);
            var comment = await _dbContext.SocialComment.FirstOrDefaultAsync(c => c.CommentId == commentId);
            if (comment != null)
            {
                var comments = await CommentUtils.JoinUser(_dbContext)
                    .Where((c) => c.comment.ParentId == comment.CommentId && !c.user.IsBanned)

                    .OrderByDescending(x => x.comment.Timestamp)
                    .Take(10)
                    .ToArrayAsync();

                var tagDict = await CommentUtils.GetAllTags(_dbContext, comments.Select(x => x.comment).ToArray());

                return comments.Select(x => new SocialCommentResponse(x.comment, x.identity, tagDict[x.comment.CommentId])).ToArray();
            }



            return StatusCode(404);
        }
        */
        /*
     [ServiceFilter(typeof(LockUserAttribute))]
     [HttpPost("{id}/social/comment/{commentId}/delete")]
     [Authorize(Policy = AuthScopes.ChangeUser)]
     public async Task<ActionResult<SocialCommentResponse>> DeleteComment([FromRoute] string id, [FromRoute] string commentId, [FromQuery] string reader)
     {
         if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

         var platformId = BitcornUtils.GetPlatformId(id);
         var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
         if (user != null && !user.IsBanned)
         {
             var comment = await _dbContext.SocialComment.FirstOrDefaultAsync(x => x.CommentId == commentId);
             if (comment != null)
             {
                 if (comment.UserId == user.UserId)
                 {
                     if (comment.IsListed)
                     {
                         comment.IsListed = false;
                         await _dbContext.SaveAsync();
                     }
                 }
             }
             var tags = new JoinedSocialTag[0];
             if (comment.IsListed)
             {
                 var queryResult = await CommentUtils.JoinTags(_dbContext).Where(c => c.Tag.CommentId == comment.CommentId).ToArrayAsync();
                 tags = queryResult.Select(x => new JoinedSocialTag(x.Tag, x.Identity)).ToArray();
             }

             return new SocialCommentResponse(comment, user.UserIdentity, tags);
         }

         return StatusCode(404);
     }

     public class ReadNotificationsBody
     {
         public string CommentId { get; set; }
     }

     [ServiceFilter(typeof(CacheUserAttribute))]
     [HttpPost("{id}/social/notifications")]
     [Authorize(Policy = AuthScopes.ChangeUser)]
     public async Task<ActionResult<object>> ReadNotifications([FromRoute] string id, [FromBody] ReadNotificationsBody body)
     {

         if (this.GetCachedUser() != null)
             throw new InvalidOperationException();
         if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

         var platformId = BitcornUtils.GetPlatformId(id);
         var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
         if (user != null)
         {
             SocialTag[] tags = null;
             if (body.CommentId == null)
             {
                 tags = await _dbContext.SocialTag.Where((s) => s.TagUserId == user.UserId && !s.Seen).ToArrayAsync();

             }
             else
             {
                 tags = await _dbContext.SocialTag.Where((s) => s.TagUserId == user.UserId && !s.Seen && s.CommentId == body.CommentId).ToArrayAsync();

             }

             for (int i = 0; i < tags.Length; i++)
             {
                 tags[i].Seen = true;
             }

             if (tags.Length > 0)
             {
                 await _dbContext.SaveAsync();
             }
             return await CommentUtils.GetNotifications(_dbContext, user);
         }

         return StatusCode(404);
     }

     [ServiceFilter(typeof(CacheUserAttribute))]
     [HttpGet("{id}/social/notifications")]
     [Authorize(Policy = AuthScopes.ChangeUser)]
     public async Task<ActionResult<object>> GetNotifications([FromRoute] string id)
     {

         if (this.GetCachedUser() != null)
             throw new InvalidOperationException();
         if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");

         var platformId = BitcornUtils.GetPlatformId(id);
         var user = await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
         if (user != null)
         {
             return await CommentUtils.GetNotifications(_dbContext, user);


         }

         return StatusCode(404);
     }
     */  /*
        [ServiceFilter(typeof(CacheUserAttribute))]
        [HttpPost("notifications")]
        [Authorize(Policy = AuthScopes.ReadUser)]
        public async Task<ActionResult<object>> GetNotifications([FromBody] BulkAuth0Request body)
        {
            if (this.GetCachedUser() != null)
                throw new InvalidOperationException();
            var ids = body.Ids;
            var users = await _dbContext.UserIdentity.Where(x => ids.Contains(x.Auth0Id)).ToArrayAsync();
            var userIds = users.Select(x => x.UserId).ToArray();
            var auth0IdDict = users.ToDictionary(x => x.UserId, x => x.Auth0Id);
            var result = await CommentUtils.GetNotifications(_dbContext, userIds);
            var output = new Dictionary<string, List<CommentUtils.TagSelect>>();
            foreach (var item in auth0IdDict)
            {
                output.Add(item.Value, new List<CommentUtils.TagSelect>());
            }

            for (int i = 0; i < result.Length; i++)
            {
                var auth0Id = auth0IdDict[result[i].TaggedId];
                if (!output.TryGetValue(auth0Id, out var list))
                {
                    list = new List<CommentUtils.TagSelect>();
                    output.Add(auth0Id, list);
                }

                list.Add(result[i]);
                //result[i].TaggedAuth0Id = auth0IdDict[result[i].TaggedId];
            }

            return output;
        }*/
        /*
              [ServiceFilter(typeof(LockUserAttribute))]
              [HttpPost("{id}/setlivestream/giveaway")]
              [Authorize(Policy = AuthScopes.ChangeUser)]
              public async Task<ActionResult<object>> SetLivestreamGiveaway([FromRoute] string id, [FromBody] SetLivestreamGiveawayBody body)
              {
                  var platformId = BitcornUtils.GetPlatformId(id);
                  var user = this.GetCachedUser();//await BitcornUtils.GetUserForPlatform(platformId, _dbContext).FirstOrDefaultAsync();
                  if (user != null)
                  {
                      var liveStream = await _dbContext.UserLivestream.FirstOrDefaultAsync(u => u.UserId == user.UserId);
                      if (liveStream != null)
                      {
                          liveStream.GiveawayEnd = DateTime.Now.AddMinutes(body.Duration >= 1 ? body.Duration : 1);
                          liveStream.GiveawayOpen = body.Open;
                          liveStream.GiveawayText = body.Text;
                          liveStream.GiveawayEntryFee = body.EntryFee;
                          liveStream.GiveawayIndex++;
                          await _dbContext.SaveAsync();
                          return new PublicLivestreamsResponse(user, liveStream, null);//new { success = true };

                      }
                  }

                  return new { success = false };


              }
              */
        /*
        [ServiceFilter(typeof(LockUserAttribute))]
        [HttpPost("{id}/setlivestream/giveaway/{channelId}/enter")]
        [Authorize(Policy = AuthScopes.ChangeUser)]
        public async Task<ActionResult<object>> SetLivestreamGiveaway([FromRoute] string id, [FromRoute] string channelId)
        {
            var platformId = BitcornUtils.GetPlatformId(id);
            var user = this.GetCachedUser();
            var channelUser = await BitcornUtils.GetUserForPlatform(BitcornUtils.GetPlatformId(channelId), _dbContext).FirstOrDefaultAsync();
            if (user != null && channelUser != null)
            {
                var liveStream = await _dbContext.UserLivestream.FirstOrDefaultAsync(u => u.UserId == channelUser.UserId);
                if (liveStream != null && user.UserId != liveStream.UserId)
                {
                    if (liveStream.GiveawayOpen)
                    {
                        var existingTicket = await _dbContext.UserGiveawayTicket.Where(x => x.UserId == user.UserId && x.ChannelId == liveStream.UserId).FirstOrDefaultAsync();
                        if (existingTicket == null)
                        {
                            existingTicket = new UserGiveawayTicket();
                            existingTicket.ChannelId = channelUser.UserId;
                            existingTicket.UserId = user.UserId;
                            existingTicket.Amount = 1;
                            existingTicket.GiveawayIndex = -1;
                            _dbContext.UserGiveawayTicket.Add(existingTicket);
                        }

                        if (existingTicket.GiveawayIndex != liveStream.GiveawayIndex)
                        {
                            existingTicket.GiveawayIndex = liveStream.GiveawayIndex;
                            await _dbContext.SaveAsync();
                            return new PublicLivestreamsResponse(user, liveStream, existingTicket);//new { success = true };

                        }
                    }

                    // await _dbContext.SaveAsync();

                }
            }

            return new { success = false };


        }
        */
    }
}
