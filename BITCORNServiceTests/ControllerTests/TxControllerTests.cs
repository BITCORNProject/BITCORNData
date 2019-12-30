using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNServiceTests.Models;
using BITCORNServiceTests.Utils;
using Microsoft.AspNetCore.Mvc;
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
                var startFromRained = startFromUser.UserStat.Rained.Value;
                var startFromTotalRained = startFromUser.UserStat.RainTotal.Value;

                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                var startToRainedOn = startToUser.UserStat.RainedOn.Value;
                var startToRainedOnTotal = startToUser.UserStat.RainedOnTotal.Value;
                var startBalance = startFromUser.UserWallet.Balance.Value;
                
                var rainAmount = 10;
                var results = await Rain(rainAmount, startFromUser, new User[] { startToUser}, true);

                using(var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                    var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                    Assert.Equal(startFromRained+1,endFromUser.UserStat.Rained.Value);
                    Assert.Equal(startFromTotalRained+rainAmount,endFromUser.UserStat.RainTotal.Value);
                    Assert.Equal(startToRainedOn+1,endToUser.UserStat.RainedOn.Value);
                    Assert.Equal(startToRainedOnTotal+rainAmount,endToUser.UserStat.RainedOnTotal.Value);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestRainSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
               
                var startBalance = startFromUser.UserWallet.Balance.Value;
                var recipientCount = 10;
                var rainAmount = 10;

                var to = dbContext.JoinUserModels().Where(u=>!u.IsBanned).Take(recipientCount).ToArray();
            
                var results = await Rain(rainAmount, startFromUser, to, true);
                var receipts = results[0].ResponseObject.Value;
                
                for (int i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
                    
                }
                var endBalance = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).Select(u => u.UserWallet.Balance.Value).FirstOrDefault();
                Assert.Equal(endBalance, startBalance - rainAmount*recipientCount);

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
                var txController = new TxController(dbContext);
                int changeCount = await txController.Payout(new HashSet<string>() {
                    _configuration["Config:TestFromUserId"],
                    _configuration["Config:TestToUserId"]
                });
                Assert.Equal(4,changeCount);
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
                    toList.Add("twitch|"+toUsers[i].UserIdentity.TwitchId);
                    results.Add(res);
                }
               
                var txController = new TxController(dbContext);
                var request = new RainRequest();
                request.Columns = new string[] {"twitchid" };
                request.Amount = amount;
                request.Platform = "twitch";

                request.From = "twitch|" + fromUser.UserIdentity.TwitchId;
                request.To = toList.ToArray();

                var response = (await txController.Rain(request));

                var fromEnd = dbContext.TwitchQuery(fromUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;//(decimal)responseData[0].From[nameof(UserWallet.Balance).ToLower()];

                foreach (var result in results)
                {
                    if (writeOutput)
                    {
                        result.FromEndBalance = fromEnd.Value;
                        
                        var toEnd = dbContext.TwitchQuery(toUsers.FirstOrDefault(u => u.UserId == result.ToUserId).UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;
                        result.ToEndBalance = toEnd.Value;
                    }
                    result.ResponseObject = response;
                }
                
                return results.ToArray();
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        async Task<TxTestResult> Tip(decimal amount, User fromUser, User toUser,bool writeOutput = true)
        {
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                
                var tipResult = new TxTestResult();
                tipResult.Amount = amount;
                tipResult.ToUserId = toUser.UserId;
                tipResult.FromUserId = fromUser.UserId;

                var txController = new TxController(dbContext);
                var request = new TipRequest();
                request.Columns = new string[] { };

                tipResult.FromStartBalance = fromUser.UserWallet.Balance.Value;
                tipResult.ToStartBalance = toUser.UserWallet.Balance.Value;

                request.Amount = amount;
                request.Platform = "twitch";

                request.From = "twitch|" + fromUser.UserIdentity.TwitchId;
                request.To = "twitch|" + toUser.UserIdentity.TwitchId;

                var response = (await txController.Tipcorn(request));
                if (writeOutput)
                {
                    var fromEnd = dbContext.TwitchQuery(fromUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;//(decimal)responseData[0].From[nameof(UserWallet.Balance).ToLower()];
                    var toEnd = dbContext.TwitchQuery(toUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;

                    tipResult.FromEndBalance = fromEnd.Value;
                    tipResult.ToEndBalance = toEnd.Value;
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
                var recipientBalances = recipients.ToDictionary(u=>u.UserId,u=>u.UserWallet.Balance);
                List<Task> tasks = new List<Task>();

                for (int j = 0; j < runs; j++)
                {
                    var task = Rain(rainAmount, startFromUser, recipients, true);
                    tasks.Add(task);
                    tasks.Add(task.ContinueWith((res) => {
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
                    var endBalance = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).Select(u => u.UserWallet.Balance.Value).FirstOrDefault();
                    Assert.Equal(endBalance, startBalance - ((rainAmount * recipientCount) * success));
                    var gain = rainAmount * success;
                    foreach (var recipient in recipients)
                    {
                        var user = await dbContext2.TwitchAsync(recipient.UserIdentity.TwitchId);
                        Assert.Equal(recipientBalances[user.UserId]+gain, user.UserWallet.Balance);
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
                var task2 = task.ContinueWith(res=> {
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
           
            result.Amount = amount*success;
            using (var dbContext2 = TestUtils.CreateDatabase())
            {
                var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                result.FromEndBalance = endFromUser.UserWallet.Balance.Value;
                result.ToEndBalance = endToUser.UserWallet.Balance.Value;
            }

            Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
            Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
        }

        [Fact]
        public async Task TestTipCornSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var tipAmount = 100;
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startFromTip = startFromUser.UserStat.Tip;
                var startFromTotalTip = startFromUser.UserStat.TipTotal;

                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                var startToUserTipped = startToUser.UserStat.Tipped;
                var startToUserTotalTipped = startToUser.UserStat.TippedTotal;


                var result = await Tip(tipAmount, startFromUser, startToUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var endFromUser = dbContext2.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                    var endToUser = dbContext2.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                    Assert.Equal(startFromTip+1,endFromUser.UserStat.Tip);
                    Assert.Equal(startFromTotalTip+tipAmount,endFromUser.UserStat.TipTotal);
                    Assert.Equal(startToUserTipped+1,endToUser.UserStat.Tipped);
                    Assert.Equal(startToUserTotalTipped+tipAmount,endToUser.UserStat.TippedTotal);
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
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                Assert.Null(result.ResponseObject.Value[0].Tx);

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
                var txController = new TxController(dbContext);
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
                Assert.Equal(startToUser,endToUser);
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
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault().UserWallet.Balance;
                var txController = new TxController(dbContext);
                var request = new TipRequest();
                request.Columns = new string[] { };
                request.To = "twitch|123123" ;
                request.From = "twitch|"+_configuration["Config:TestFromUserId"];
                request.Platform = "twitch";
                request.Amount = 100;
                var response = await txController.Tipcorn(request);
                var receipt = response.Value[0];
                Assert.Null(receipt.Tx);
                Assert.NotNull(receipt.From);
                Assert.Null(receipt.To);

                var endFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault().UserWallet.Balance;
                Assert.Equal(startFromUser,endFromUser);
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
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var result = await Tip(100, fromUser, toUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                Assert.Null(result.ResponseObject.Value[0].Tx);

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
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestBannedUser"]).FirstOrDefault();

                var result = await Tip(100, fromUser, toUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance);
                Assert.Null(result.ResponseObject.Value[0].Tx);

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
                var results = await Rain(startToUser.UserWallet.Balance.Value+1, startFromUser, new User[] { startToUser }, true);
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

                var result = await Tip(fromUser.UserWallet.Balance.Value+1, fromUser, toUser);
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
        public async Task TestWithdrawSuccess()
        {
            /*
            var withdrawUser = new WithdrawRequest()
            {
                Id = _configuration["Config:TestUserId"],
                Amount = 1,
                CornAddy = _configuration["Config:TestAddress"]
            };

            using (var dbContext = TestUtils.CreateDatabase())
            {
                var txController = new TxController(dbContext);

                var startUser = await dbContext.TwitchAsync(withdrawUser.Id);
                var startBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                await txController.Withdraw(withdrawUser);

                var endBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                Assert.True(startBalance - withdrawUser.Amount == endBalance);
            }*/
        }
    }
}
