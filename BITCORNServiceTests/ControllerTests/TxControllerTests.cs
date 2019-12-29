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
        public async Task TestRainSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var startFromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var startToUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();
                var results = await Rain(10,startFromUser,new User[] { startToUser },true);
                var result = results[0];
                Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
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
                var fromUser = dbContext.TwitchQuery(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchQuery(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var result = await Tip(100, fromUser, toUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.Amount);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.Amount);
               
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
