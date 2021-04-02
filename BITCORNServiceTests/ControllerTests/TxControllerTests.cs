using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Platforms;
using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Twitch;
using BITCORNService.Utils.Tx;
using BITCORNService.Utils.Wallet;
using BITCORNServiceTests.Models;
using BITCORNServiceTests.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace BITCORNServiceTests
{
    public class TxControllerTests
    {
        private IConfigurationRoot _configuration;
        public TxControllerTests()
        {
            _configuration = TestUtils.GetConfig();
        }
        [Fact]
        public async Task TestRainAnalytics()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startFromRained = startFromUser.UserStat.AmountOfRainsSent.Value;
                var startFromTotalRained = startFromUser.UserStat.TotalSentBitcornViaRains.Value;

                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                var startToRainedOn = startToUser.UserStat.AmountOfRainsReceived.Value;
                var startToRainedOnTotal = startToUser.UserStat.TotalReceivedBitcornRains.Value;
                var startBalance = startFromUser.UserWallet.Balance.Value;

                var rainAmount = 10;
                var results = await Rain(rainAmount, startFromUser, new User[] { startToUser }, true);

                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                    var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                    Assert.Equal(startFromRained + 1, endFromUser.UserStat.AmountOfRainsSent.Value);
                    Assert.Equal(startFromTotalRained + rainAmount, endFromUser.UserStat.TotalSentBitcornViaRains.Value);
                    Assert.Equal(startToRainedOn + 1, endToUser.UserStat.AmountOfRainsReceived.Value);
                    Assert.Equal(startToRainedOnTotal + rainAmount, endToUser.UserStat.TotalReceivedBitcornRains.Value);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task Testbuycorn()
        {
            UnlockTestUser();
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startBal = startFromUser.UserWallet.Balance.Value;
                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                var usdAmount = 10;
                var prices = await ProbitApi.GetCornPriceAsync(dbContext);
                var response = await txController.CanBuycorn(startFromUser.UserIdentity.Auth0Id, usdAmount);
                if (response.Value.Success)
                {
                    var cornAmount = usdAmount / prices;
                    string token = response.Value.Token;
                    var paymentId = Guid.NewGuid().ToString();
                    var orderId = Guid.NewGuid().ToString();
                    var buyCornResponse = await txController.Buycorn(new TxController.BuyCornRequest()
                    {
                        Auth0Id = startFromUser.UserIdentity.Auth0Id,
                        CornAmount = cornAmount,
                        UsdAmount = usdAmount,
                        CreatedAt = DateTime.Now,
                        Fingerprint = "123",
                        OrderId = orderId,
                        PaymentId = paymentId,
                        ReceiptNumber = "123",
                        Token = token
                    });

                    var cv = buyCornResponse.Value;
                    if (cv.Success)
                    {
                        var closeResponse = await txController.CloseBuycorn(new TxController.CompleteBuyCornRequest()
                        {
                            Auth0Id = startFromUser.UserIdentity.Auth0Id,

                            CornPurchaseId = cv.PurchaseCloseId,
                            PaymentId = paymentId,
                            Token = token
                        });

                        Assert.True(closeResponse.Value.Success);
                        Assert.True(closeResponse.Value.PurchaseCloseId != null && closeResponse.Value.PurchaseCloseId != 0);
                        {
                            var dbContext2 = TestUtils.CreateDatabase();
                            var startFromUser2 = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).Select(x=>x.UserWallet.Balance).FirstOrDefault();
                            Assert.True(startFromUser2>startBal);
                        }
                        return;
                    }
                }
                Assert.False(true);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestRainSuccess()
        {
            UnlockTestUser();
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();

                var startBalance = startFromUser.UserWallet.Balance.Value;
                var recipientCount = 10;
                var rainAmount = 10;

                var to = dbContext.JoinUserModels().Where(u => !u.IsBanned).Take(recipientCount).ToArray();

                var results = await Rain(rainAmount, startFromUser, to, true);
                var receipts = results[0].ResponseObject.Value;

                for (int i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);

                }
                var endBalance = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).Select(u => u.UserWallet.Balance.Value).FirstOrDefault();
                Assert.Equal(endBalance, startBalance - rainAmount * recipientCount);

            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestPayoutSuccess()
        {

            var dbContext = TestUtils.CreateDatabase();
            try
            {

                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                //context.Items.Add("user", dbContext.JoinUserModels().FirstOrDefault(u => u.UserId == TxUtils.BitcornHubPK));
                var startBalance = (await TxUtils.GetBitcornhub(dbContext)).UserWallet.Balance;
                var changeCount = (await txController.Payout(new PayoutRequest()
                {
                    Chatters = new HashSet<string>() { _configuration["Config:TestFromUserId"], _configuration["Config:TestToUserId"] },
                    Minutes = 1,
                    IrcTarget = "75987197"

                })).Value;

                Assert.True((int)changeCount > 0);
                {
                    var db2 = TestUtils.CreateDatabase();
                    var endBalance = (await TxUtils.GetBitcornhub(db2)).UserWallet.Balance;
                    Assert.True(startBalance > endBalance);
                    //Assert.Equal(2, (int)changeCount);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        async Task<TxTestResult[]> Rain(decimal amount, User fromUser, User[] toUsers, bool writeOutput = true)
        {
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                List<TxTestResult> results = new List<TxTestResult>();
                List<string> toList = new List<string>();
                for (int i = 0; i < toUsers.Length; i++)
                {
                    var res = new TxTestResult();
                    res.Amount = amount;
                    res.FromUserId = fromUser.UserId;
                    res.ToUserId = toUsers[i].UserId;
                    res.FromStartBalance = fromUser.UserWallet.Balance.Value;
                    res.ToStartBalance = toUsers[i].UserWallet.Balance.Value;
                    toList.Add("twitch|" + toUsers[i].UserIdentity.TwitchId);
                    results.Add(res);
                }

                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", fromUser);

                var request = new RainRequest();
                request.Columns = new string[] { "twitchid" };
                request.Amount = amount;
                request.Platform = "twitch";

                request.From = "twitch|" + fromUser.UserIdentity.TwitchId;
                request.To = toList.ToArray();
                request.IrcTarget = "120524051";
                request.IrcMessage = "test message";
                var response = (await txController.Rain(request));
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var fromEnd = dbContext2.TwitchQuery(fromUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;//(decimal)responseData[0].From[nameof(UserWallet.Balance).ToLower()];

                    foreach (var result in results)
                    {
                        if (writeOutput)
                        {
                            result.FromEndBalance = fromEnd.Value;

                            var toEnd = dbContext2.TwitchQuery(toUsers.FirstOrDefault(u => u.UserId == result.ToUserId).UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;
                            result.ToEndBalance = toEnd.Value;
                        }
                        result.ResponseObject = response;
                    }
                }
                return results.ToArray();
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        async Task<TxTestResult> Tip(decimal amount, User fromUser, User toUser, bool writeOutput = true)
        {
            var dbContext = TestUtils.CreateDatabase();

            try
            {

                var tipResult = new TxTestResult();
                tipResult.Amount = amount;
                tipResult.ToUserId = toUser.UserId;
                tipResult.FromUserId = fromUser.UserId;

                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", fromUser);

                var request = new TipRequest();
                request.Columns = new string[] { };
                //request.IrcTarget = "#markettraderstv";
                tipResult.FromStartBalance = fromUser.UserWallet.Balance.Value;
                tipResult.ToStartBalance = toUser.UserWallet.Balance.Value;

                request.Amount = amount;
                request.Platform = "twitch";
                request.IrcTarget = "120524051";
                request.IrcMessage = "test message";
                request.From = "twitch|" + fromUser.UserIdentity.TwitchId;
                request.To = "twitch|" + toUser.UserIdentity.TwitchId;

                var response = (await txController.Tipcorn(request));
                if (writeOutput)
                {
                    using (var dbContext2 = TestUtils.CreateDatabase())
                    {
                        var fromEnd = dbContext2.TwitchQuery(fromUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;//(decimal)responseData[0].From[nameof(UserWallet.Balance).ToLower()];
                        var toEnd = dbContext2.TwitchQuery(toUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;

                        tipResult.FromEndBalance = fromEnd.Value;
                        tipResult.ToEndBalance = toEnd.Value;
                    }
                }
                tipResult.ResponseObject = response;
                return tipResult;
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestManyRains()
        {
            UnlockTestUser();

            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startBalance = startFromUser.UserWallet.Balance.Value;
                var recipientCount = 10;
                var rainAmount = 10;
                var runs = 5;
                int success = 0;
                var recipients = dbContext.JoinUserModels().Where(u => !u.IsBanned).Take(recipientCount).ToArray();
                var recipientBalances = recipients.ToDictionary(u => u.UserId, u => u.UserWallet.Balance);
                List<Task> tasks = new List<Task>();

                for (int j = 0; j < runs; j++)
                {
                    var task = Rain(rainAmount, startFromUser, recipients, true);
                    tasks.Add(task);
                    tasks.Add(task.ContinueWith((res) =>
                    {
                        if (res.Result[0].ResponseObject.Result == null)
                        {
                            if (res.Result[0].ResponseObject.Value[0].Tx != null)
                            {
                                success++;
                            }
                        }
                    }));

                }
                await Task.WhenAll(tasks);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    Assert.False(success == 0);

                    Assert.Equal(runs, success);
                    var endBalance = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).Select(u => u.UserWallet.Balance.Value).FirstOrDefault();
                    Assert.Equal(endBalance, startBalance - ((rainAmount * recipientCount) * success));
                    var gain = rainAmount * success;
                    foreach (var recipient in recipients)
                    {
                        var user = await dbContext2.TwitchAsync(recipient.UserIdentity.TwitchId);
                        Assert.Equal(recipientBalances[user.UserId] + gain, user.UserWallet.Balance);
                    }
                }
            }

            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestManyTips()
        {
            UnlockTestUser();

            decimal amount = 1;
            int count = 10;
            decimal totalTipAmount = amount * count;
            User startFromUser = null;
            User startToUser = null;
            var result = new TxTestResult();

            using (var dbContext = TestUtils.CreateDatabase())
            {
                startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                result.FromStartBalance = startFromUser.UserWallet.Balance.Value;
                result.ToStartBalance = startToUser.UserWallet.Balance.Value;
            }

            int success = 0;
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                var task = Tip(amount, startFromUser, startToUser);
                var task2 = task.ContinueWith(res =>
                {
                    if (res.Result.ResponseObject.Result == null)
                    {
                        if (res.Result.ResponseObject.Value[0].Tx != null)
                        {
                            success++;
                        }
                    }
                });
                tasks.Add(task);
                tasks.Add(task2);
            }
            await Task.WhenAll(tasks);

            result.Amount = amount * success;
            using (var dbContext2 = TestUtils.CreateDatabase())
            {
                Assert.False(success == 0);
                Assert.Equal(count, success);
                var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                result.FromEndBalance = endFromUser.UserWallet.Balance.Value;
                result.ToEndBalance = endToUser.UserWallet.Balance.Value;
            }

            Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
            Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
        }

        void UnlockTestUser()
        {
            using (var db = TestUtils.CreateDatabase())
            {
                db.Database.ExecuteSqlRaw("update userwallet set islocked = 0 where userid = 1722");
            }
        }

        [Fact]
        public async Task TestTipCornSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            UnlockTestUser();

            try
            {
                var tipAmount = 10;
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startFromTip = startFromUser.UserStat.AmountOfTipsSent;
                var startFromTotalTip = startFromUser.UserStat.TotalSentBitcornViaTips;

                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                var startToUserTipped = startToUser.UserStat.AmountOfTipsReceived;
                var startToUserTotalTipped = startToUser.UserStat.TotalReceivedBitcornTips;


                var result = await Tip(tipAmount, startFromUser, startToUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                    var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                    Assert.Equal(startFromTip + 1, endFromUser.UserStat.AmountOfTipsSent);
                    Assert.Equal(startFromTotalTip + tipAmount, endFromUser.UserStat.TotalSentBitcornViaTips);
                    Assert.Equal(startToUserTipped + 1, endToUser.UserStat.AmountOfTipsReceived);
                    Assert.Equal(startToUserTotalTipped + tipAmount, endToUser.UserStat.TotalReceivedBitcornTips);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestBitdonationSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            UnlockTestUser();

            try
            {
                var tipAmount = 10;
                var startFromUser = await TxUtils.GetBitcornhub(dbContext);//dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();


                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", startFromUser);

                var request = new BitDonationRequest();
                request.Columns = new string[] { };
                var startToBal = startToUser.UserWallet.Balance;
                var startFromBal = startFromUser.UserWallet.Balance;
                //request.IrcTarget = "#markettraderstv";
                //tipResult.FromStartBalance = startFromUser.UserWallet.Balance.Value;
                //tipResult.ToStartBalance = startToUser.UserWallet.Balance.Value;

                //request.Amount = amount;
                request.Platform = "twitch";
                request.IrcTarget = "120524051";
                var livestream = dbContext.UserLivestream.Where(x => x.UserId == 1722).FirstOrDefault();
                request.IrcMessage = "test message";
                request.From = "twitch|" + startFromUser.UserIdentity.TwitchId;
                request.To = "twitch|" + startToUser.UserIdentity.TwitchId;
                request.BitAmount = 10;
                var result = (await txController.BitDonation(request)).Value;

                //var result = await Tip(tipAmount, startFromUser, startToUser);
                //Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
                //Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = await TxUtils.GetBitcornhub(dbContext2);//dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                    var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).Select(x => x.UserWallet.Balance).FirstOrDefault();
                    var balDiff = livestream.BitcornPerBit * request.BitAmount;
                    var totalDiff = endToUser - startToBal;
                    Assert.Equal(startToBal + balDiff, endToUser);
                    Assert.Equal(startFromBal, endFromUser.UserWallet.Balance + balDiff);
                    /*
                    Assert.Equal(startFromTip + 1, endFromUser.UserStat.AmountOfTipsSent);
                    Assert.Equal(startFromTotalTip + tipAmount, endFromUser.UserStat.TotalSentBitcornViaTips);
                    Assert.Equal(startToUserTipped + 1, endToUser.UserStat.AmountOfTipsReceived);
                    Assert.Equal(startToUserTotalTipped + tipAmount, endToUser.UserStat.TotalReceivedBitcornTips);
                */
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }


        [Fact]
        public async Task TestChannelpointsSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            UnlockTestUser();

            try
            {
                var tipAmount = 10;
                var startFromUser = await TxUtils.GetBitcornhub(dbContext);//dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();


                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", startFromUser);

                var request = new ChannelPointsRedemptionRequest();
                request.Columns = new string[] { };
                var startToBal = startToUser.UserWallet.Balance;
                var startFromBal = startFromUser.UserWallet.Balance;
                //request.IrcTarget = "#markettraderstv";
                //tipResult.FromStartBalance = startFromUser.UserWallet.Balance.Value;
                //tipResult.ToStartBalance = startToUser.UserWallet.Balance.Value;

                //request.Amount = amount;
                request.Platform = "twitch";
                request.IrcTarget = "120524051";
                var livestream = dbContext.UserLivestream.Where(x => x.UserId == 1722).FirstOrDefault();
                request.IrcMessage = "test message";
                request.From = "twitch|" + startFromUser.UserIdentity.TwitchId;
                request.To = "twitch|" + startToUser.UserIdentity.TwitchId;
                request.ChannelPointAmount = 1000;
                var result = (await txController.RedeemChannelpoints(request)).Value;

                //var result = await Tip(tipAmount, startFromUser, startToUser);
                //Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
                //Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = await TxUtils.GetBitcornhub(dbContext2);//dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                    var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).Select(x => x.UserWallet.Balance).FirstOrDefault();
                    var balDiff = livestream.BitcornPerChannelpointsRedemption * request.ChannelPointAmount;
                    var totalDiff = endToUser - startToBal;
                    Assert.Equal(startToBal + balDiff, endToUser);
                    Assert.Equal(startFromBal, endFromUser.UserWallet.Balance + balDiff);
                    /*
                    Assert.Equal(startFromTip + 1, endFromUser.UserStat.AmountOfTipsSent);
                    Assert.Equal(startFromTotalTip + tipAmount, endFromUser.UserStat.TotalSentBitcornViaTips);
                    Assert.Equal(startToUserTipped + 1, endToUser.UserStat.AmountOfTipsReceived);
                    Assert.Equal(startToUserTotalTipped + tipAmount, endToUser.UserStat.TotalReceivedBitcornTips);
                */
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestSubEventSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            UnlockTestUser();

            try
            {
                var tipAmount = 10;
                var startFromUser = await TxUtils.GetBitcornhub(dbContext);//dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();


                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", startFromUser);

                var request = new ChannelSubRequest();
                request.Columns = new string[] { };
                var startToBal = startToUser.UserWallet.Balance;
                var startFromBal = startFromUser.UserWallet.Balance;
                //request.IrcTarget = "#markettraderstv";
                //tipResult.FromStartBalance = startFromUser.UserWallet.Balance.Value;
                //tipResult.ToStartBalance = startToUser.UserWallet.Balance.Value;

                //request.Amount = amount;
                request.Platform = "twitch";
                request.IrcTarget = "120524051";
                var livestream = dbContext.UserLivestream.Where(x => x.UserId == 1722).FirstOrDefault();
                request.IrcMessage = "test message";
                request.From = "twitch|" + startFromUser.UserIdentity.TwitchId;
                request.To = "twitch|" + startToUser.UserIdentity.TwitchId;
                request.SubTier = "1000";
                var result = (await txController.SubEvent(request)).Value;

                //var result = await Tip(tipAmount, startFromUser, startToUser);
                //Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
                //Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = await TxUtils.GetBitcornhub(dbContext2);//dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                    var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).Select(x => x.UserWallet.Balance).FirstOrDefault();
                    var balDiff = livestream.Tier1SubReward;
                    var totalDiff = endToUser - startToBal;
                    Assert.Equal(startToBal + balDiff, endToUser);
                    Assert.Equal(startFromBal, endFromUser.UserWallet.Balance + balDiff);
                    /*
                    Assert.Equal(startFromTip + 1, endFromUser.UserStat.AmountOfTipsSent);
                    Assert.Equal(startFromTotalTip + tipAmount, endFromUser.UserStat.TotalSentBitcornViaTips);
                    Assert.Equal(startToUserTipped + 1, endToUser.UserStat.AmountOfTipsReceived);
                    Assert.Equal(startToUserTotalTipped + tipAmount, endToUser.UserStat.TotalReceivedBitcornTips);
                */
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestTipCornUserGetsLocked()
        {
            var dbContext = TestUtils.CreateDatabase();
            UnlockTestUser();

            try
            {
                var tipAmount = 30_000_010;
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startFromTip = startFromUser.UserStat.AmountOfTipsSent;
                var startFromTotalTip = startFromUser.UserStat.TotalSentBitcornViaTips;

                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();


                var result = await Tip(tipAmount, startFromUser, startToUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);

            }
            finally
            {
                dbContext.Dispose();
            }

            var dbContext2 = TestUtils.CreateDatabase();
            var islocked = dbContext2.UserWallet.Where(x => x.UserId == 1722).FirstOrDefault().IsLocked;
            Assert.Equal(true, islocked);
            dbContext2.Dispose();
        }

        [Fact]
        public async Task TestRainUserGetsLocked()
        {
            var dbContext = TestUtils.CreateDatabase();
            UnlockTestUser();

            try
            {
                var tipAmount = 30_000_010;
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startFromTip = startFromUser.UserStat.AmountOfTipsSent;
                var startFromTotalTip = startFromUser.UserStat.TotalSentBitcornViaTips;

                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();


                var r = await Rain(tipAmount, startFromUser, new User[] { startToUser });
                var result = r[0];
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);

            }
            finally
            {
                dbContext.Dispose();
            }

            var dbContext2 = TestUtils.CreateDatabase();
            var islocked = dbContext2.UserWallet.Where(x => x.UserId == 1722).FirstOrDefault().IsLocked;
            Assert.Equal(true, islocked);
            dbContext2.Dispose();
        }
        [Fact]
        public async Task TestTipCornFailureWithLock()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var tipAmount = 10;
                using (var db = TestUtils.CreateDatabase())
                {
                    db.Database.ExecuteSqlRaw("update userwallet set islocked = 1 where userid = 1722");
                    //db.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault().UserWallet.IsLocked = true;

                }
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();


                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var result = await Tip(tipAmount, startFromUser, startToUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                using (var db = TestUtils.CreateDatabase())
                {
                    db.Database.ExecuteSqlRaw("update userwallet set islocked = 0 where userid = 1722");
                }

            }
            finally
            {
                dbContext.Dispose();
            }
        }


        [Fact]
        public async Task TestTipCornSelf()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();

                var result = await Tip(100, fromUser, toUser);

                Assert.Equal((result.ResponseObject.Result as StatusCodeResult).StatusCode, (int)HttpStatusCode.BadRequest);

            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestTipCornFromUnregistered()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault().UserWallet.Balance;
                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", null);
                var request = new TipRequest();
                request.Columns = new string[] { };
                request.To = "twitch|" + _configuration["Config:TestToUserId"];
                request.From = "twitch|123123";
                request.Platform = "twitch";
                request.Amount = 100;
                var response = await txController.Tipcorn(request);
                var receipt = response.Value[0];
                Assert.Null(receipt.Tx);
                Assert.Null(receipt.From);
                Assert.NotNull(receipt.To);
                var endToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault().UserWallet.Balance;
                Assert.Equal(startToUser, endToUser);
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestTipCornRefund()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startFromUser = fromUser.UserWallet.Balance;
                var txController = new TxController(_configuration, dbContext);
                txController.TimeToClaimTipMinutes = -1;
                var request = new TipRequest();
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", fromUser);

                request.Columns = new string[] { };
                string toTwitchId = "123123";
                request.To = "twitch|" + toTwitchId;
                request.From = "twitch|" + _configuration["Config:TestFromUserId"];
                request.Platform = "twitch";
                request.Amount = 100;

                var toUser = await dbContext.TwitchAsync(toTwitchId);
                dbContext.RemoveRange(dbContext.UnclaimedTx);
                TestUtils.RemoveUserIfExists(dbContext, toUser);

                var response = await txController.Tipcorn(request);
                var receipt = response.Value[0];
                Assert.Null(receipt.Tx);
                Assert.NotNull(receipt.From);
                Assert.Null(receipt.To);

                using (var dbContext2 = TestUtils.CreateDatabase())
                {

                    var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault().UserWallet.Balance;
                    Assert.Equal(startFromUser - request.Amount, endFromUser);
                    await TxUtils.RefundUnclaimed(dbContext, 0);
                    /*
                */
                }
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var balance = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault().UserWallet.Balance;
                    Assert.Equal(startFromUser, balance);
                    dbContext2.AddUser(new UserIdentity()
                    {
                        Auth0Id = "temp",
                        Auth0Nickname = "temp",
                        TwitchId = toTwitchId
                    }, new UserWallet());
                    var to = await dbContext2.TwitchAsync(toTwitchId);
                    int claim = await TxUtils.TryClaimTx(new PlatformId() { Platform = "twitch", Id = toTwitchId }, to, dbContext2);
                    Assert.Equal(0, claim);
                }
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var to = await dbContext2.TwitchAsync(toTwitchId);
                    Assert.Equal(0, to.UserWallet.Balance);
                    TestUtils.RemoveUserIfExists(dbContext, to);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestTipCornToUnregistered()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startFromUser = fromUser.UserWallet.Balance;
                var txController = new TxController(_configuration, dbContext);
                var context = txController.ControllerContext.HttpContext = new DefaultHttpContext();
                context.Items.Add("user", fromUser);

                var request = new TipRequest();
                request.Columns = new string[] { };
                string toTwitchId = "123123";
                request.To = "twitch|" + toTwitchId;
                request.From = "twitch|" + _configuration["Config:TestFromUserId"];
                request.Platform = "twitch";
                request.Amount = 100;

                var toUser = await dbContext.TwitchAsync(toTwitchId);
                dbContext.RemoveRange(dbContext.UnclaimedTx);
                TestUtils.RemoveUserIfExists(dbContext, toUser);

                var response = await txController.Tipcorn(request);
                var receipt = response.Value[0];
                Assert.Null(receipt.Tx);
                Assert.NotNull(receipt.From);
                Assert.Null(receipt.To);

                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault().UserWallet.Balance;
                    Assert.Equal(startFromUser - request.Amount, endFromUser);

                    dbContext2.AddUser(new UserIdentity()
                    {
                        Auth0Id = "temp",
                        Auth0Nickname = "temp",
                        TwitchId = toTwitchId
                    }, new UserWallet());
                    var to = await dbContext2.TwitchAsync(toTwitchId);
                    await TxUtils.TryClaimTx(new PlatformId() { Platform = "twitch", Id = toTwitchId }, to, dbContext2);
                }
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var to = await dbContext2.TwitchAsync(toTwitchId);
                    Assert.Equal(request.Amount, to.UserWallet.Balance);
                    var claim2 = await TxUtils.TryClaimTx(new PlatformId() { Platform = "twitch", Id = toTwitchId }, to, dbContext2); ;
                    Assert.Equal(0, claim2);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestTipCornFromBannedUser()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                using (var db = TestUtils.CreateDatabase())
                {
                    var user = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();
                    user.IsBanned = true;
                    db.Update(user);
                    db.SaveChanges();
                }
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var result = await Tip(100, fromUser, toUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                Assert.Null(result.ResponseObject.Value[0].Tx);
                using (var db = TestUtils.CreateDatabase())
                {
                    var user = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();
                    user.IsBanned = false;
                    db.Update(user);
                    db.SaveChanges();
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestTipCornToBannedUser()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                using (var db = TestUtils.CreateDatabase())
                {
                    var user = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();
                    user.IsBanned = true;
                    db.Update(user);
                    db.SaveChanges();
                }
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();

                var result = await Tip(100, fromUser, toUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                Assert.Null(result.ResponseObject.Value[0].Tx);
                using (var db = TestUtils.CreateDatabase())
                {
                    var user = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();
                    user.IsBanned = false;
                    db.Update(user);
                    db.SaveChanges();
                }

            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestRainNegativeAmount()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                var results = await Rain(-1, startFromUser, new User[] { startToUser }, true);
                var result = results[0];
                Assert.Equal((result.ResponseObject.Result as StatusCodeResult).StatusCode, (int)HttpStatusCode.BadRequest);
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestRainInsufficientFunds()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                var results = await Rain(startFromUser.UserWallet.Balance.Value + 1, startFromUser, new User[] { startToUser }, true);
                var result = results[0];
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Null(result.ResponseObject.Value[0].Tx);
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestTipCornNegativeAmount()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var result = await Tip(-1, fromUser, toUser);
                Assert.Equal((result.ResponseObject.Result as StatusCodeResult).StatusCode, (int)HttpStatusCode.BadRequest);

            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestTipCornInsufficientFunds()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var result = await Tip(fromUser.UserWallet.Balance.Value + 1, fromUser, toUser);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Null(result.ResponseObject.Value[0].Tx);
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestWithdrawDebit()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var amount = 10;
                var user = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var server = dbContext.WalletServer.FirstOrDefault(u => u.Index == user.UserWallet.WalletServer);

                await WalletUtils.DebitWithdrawTx("testaddr", "test", user, server, amount, dbContext, "test", int.Parse(_configuration["Config:EmptyUserId"]));

                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var user2 = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();

                    var server2 = dbContext2.WalletServer.FirstOrDefault(u => u.Index == user.UserWallet.WalletServer);
                    Assert.Equal(user.UserWallet.Balance - amount, user2.UserWallet.Balance);
                    Assert.Equal(server.ServerBalance - amount, server2.ServerBalance);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestWithdrawInsufficientFunds()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var user = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();

                var amount = user.UserWallet.Balance.Value + 10;
                var server = dbContext.WalletServer.FirstOrDefault(u => u.Index == user.UserWallet.WalletServer);
                await WalletUtils.DebitWithdrawTx("test addr", "test", user, server, amount, dbContext, "test", int.Parse(_configuration["Config:EmptyUserId"]));

                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var user2 = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();

                    var server2 = dbContext2.WalletServer.FirstOrDefault(u => u.Index == user.UserWallet.WalletServer);
                    Assert.Equal(user.UserWallet.Balance, user2.UserWallet.Balance);
                    Assert.Equal(server.ServerBalance, server2.ServerBalance);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
    }
}
