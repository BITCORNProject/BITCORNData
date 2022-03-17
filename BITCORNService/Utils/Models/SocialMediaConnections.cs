using BITCORNService.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BITCORNService.Utils.Models
{
    public class SocialMediaConnections 
    {
        public SocialMediaConnection Bitcorn { get; set; }

        public SocialMediaConnection Twitch { get; set; }
        public SocialMediaConnection Discord { get; set; }
        public SocialMediaConnection Twitter { get; set; }
        public SocialMediaConnection Reddit { get; set; }
        public SocialMediaConnection Stream { get; set; }
        public SocialMediaConnection Rally { get; set; }
        public SocialMediaConnection Youtube { get; set; }
        public static SocialMediaConnections FromUser(User user)
        {
            return new SocialMediaConnections
            {
                Rally = new SocialMediaConnection()
                {
                    Value = user.UserIdentity.RallyId,
                    Name = user.UserIdentity.RallyUsername
                },
                Youtube = new SocialMediaConnection()
                {
                    Value = user.UserIdentity.YoutubeId,
                    Name = user.UserIdentity.YoutubeUsername
                },
                Bitcorn = new SocialMediaConnection
                {
                    Name = user.UserWallet.CornAddy,
                    Value = user.UserWallet.CornAddy
                },

                Twitch = new SocialMediaConnection
                {
                    Value = user.UserIdentity.TwitchId,
                    Name = user.UserIdentity.TwitchUsername,

                },

                Discord = new SocialMediaConnection
                {
                    Value = user.UserIdentity.DiscordId,
                    Name = user.UserIdentity.DiscordUsername,

                },

                Reddit = new SocialMediaConnection
                {
                    Value = user.UserIdentity.RedditId,
                    Name = user.UserIdentity.RedditUsername
                },

                Twitter = new SocialMediaConnection
                {
                    Value = user.UserIdentity.TwitterId,
                    Name = user.UserIdentity.TwitterUsername,

                },

                Stream = new SocialMediaConnection
                {
                    Name = "",
                    Value = !string.IsNullOrEmpty(user.UserIdentity.TwitchRefreshToken)? user.UserIdentity.TwitchId : null
                }

               
            };
        }
    }
}
