using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Reflection;
using BITCORNService.Utils.DbActions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService.Utils
{
    public class LivestreamUtils
    {
        public static object GetLivestreamSettingsForUser(User user, UserLivestream stream, Dictionary<string, object> extraColumns)
        {
            if (extraColumns == null) extraColumns = new Dictionary<string, object>();
            /*
            return new
            {
                minRainAmount = stream.MinRainAmount,
                minTipAmount = stream.MinTipAmount,
                rainAlgorithm = stream.RainAlgorithm,
                ircTarget = userIdentity.TwitchId,//stream.Stream.IrcTarget,
                txMessages = stream.TxMessages,
                txCooldownPerUser = stream.TxCooldownPerUser,
                enableTransactions = stream.EnableTransactions,
                ircEventPayments = stream.IrcEventPayments,
                bitcornhubFunded = stream.BitcornhubFunded,
                bitcornPerBit = stream.BitcornPerBit,
                bitcornPerDonation = stream.BitcornPerDonation,
                twitchRefreshToken = userIdentity.TwitchRefreshToken,
                bitcornPerChannelpointsRedemption = stream.BitcornPerChannelpointsRedemption,
                enableChannelpoints = stream.EnableChannelpoints,
                twitchUsername = userIdentity.TwitchUsername,
                channelPointCardId=stream.ChannelPointCardId


            };
            */
            var userIdentity = user.UserIdentity;
            var output = new Dictionary<string, object>
            {
                {"minRainAmount", stream.MinRainAmount },
                {"minTipAmount", stream.MinTipAmount},
                {"rainAlgorithm", stream.RainAlgorithm },
                {"ircTarget", userIdentity.TwitchId },//stream.Stream.IrcTarget,
                {"txMessages", stream.TxMessages },
                {"txCooldownPerUser", stream.TxCooldownPerUser },
                {"enableTransactions", stream.EnableTransactions },
                {"ircEventPayments", stream.IrcEventPayments },
                {"bitcornhubFunded", stream.BitcornhubFunded },
                {"bitcornPerBit", stream.BitcornPerBit },
                {"tier1SubReward", stream.Tier1SubReward },

                {"tier2SubReward", stream.Tier2SubReward },

                {"tier3SubReward", stream.Tier3SubReward },
                {"twitchRefreshToken", userIdentity.TwitchRefreshToken },
                {"bitcornPerChannelpointsRedemption", stream.BitcornPerChannelpointsRedemption },
                {"enableChannelpoints", stream.EnableChannelpoints },
                {"twitchUsername", userIdentity.TwitchUsername },
                {"channelPointCardId", stream.ChannelPointCardId },
                { "enableTts", stream.EnableTts}

            };
            if (extraColumns != null)
            {
                foreach (var item in extraColumns)
                {
                    if (!output.ContainsKey(item.Key))
                    {
                        output.Add(item.Key, item.Value);
                    }
                }
            }
            /*
            for (int i = 0; i < columns.Length; i++)
            {
                UserReflection.GetColumns(_dbContext, columns, new int[] { });
            }
            */
            return output;
        }

        internal static async Task<object[]> GetLivestreamSettings(BitcornContext dbContext, string[] columns)
        {
            if (columns == null)
            {
                columns = new string[0];
            }
            var streams = await dbContext.GetLivestreams().Where(e => e.Stream.Enabled).ToArrayAsync();
            List<object> output = new List<object>();
            var userIds = streams.Select(x => x.User.UserId).ToArray();

            //var selectColumns = columns.Split(" ");
            var selectedColumns = await UserReflection.GetColumns(dbContext, columns, userIds, new UserReflectionContext(UserReflection.StreamerModel));//new Dictionary<string,object>();
            foreach (var entry in streams)
            {
                if (!string.IsNullOrEmpty(entry.User.UserIdentity.TwitchId))
                {
                    var srcColumns = new Dictionary<string, object>();
                    try
                    {
                        if (selectedColumns.TryGetValue(entry.User.UserId, out var cols))
                        {
                            if (cols != null)
                            {
                                srcColumns = cols;
                            }
                        }
                    }
                    catch
                    {

                    }
                    output.Add(LivestreamUtils.GetLivestreamSettingsForUser(entry.User, entry.Stream, srcColumns));
                    //channels.Add(stream.UserIdentity.TwitchUsername);
                }

            }

            return output.ToArray();
        }
        /*
        internal static async Task<bool> HandleTts(BitcornContext dbContext, User fromUser, User toUser, UserStreamAction streamAction, bool saveChanges)
        {
           
            var userTts = await dbContext.UserTts.FirstOrDefaultAsync(x => x.UserId == fromUser.UserId);
            if (userTts == null)
            {
                userTts = new UserTts();
                userTts.Rate = 1;
                userTts.Pitch = 1;
                userTts.Voice = 0;
                userTts.UserId = fromUser.UserId;
                dbContext.UserTts.Add(userTts);
            }
           
            var socketSuccess = await WebSocketsController.TryBroadcastToBitcornfarms(dbContext, "tts", new
            {
                auth0Id = toUser.UserIdentity.Auth0Id,
                rate = userTts.Rate,
                pitch = userTts.Pitch,
                voice = userTts.Voice,
                message = streamAction.Content,
                twitchUsername = fromUser.UserIdentity.TwitchUsername
            });

            if(socketSuccess == WebSocketsController.SocketBroadcastResult.Success)
            {
                streamAction.Closed = true;
                userTts.Uses++;
                if (saveChanges)
                {
                    await dbContext.SaveAsync();
                    
                }
                return true;
            }
            else
            {
                await BITCORNLogger.LogError(dbContext, new Exception("invalid socket send result: "+socketSuccess),"");
            }
            return false;
        }*/

    }
}
