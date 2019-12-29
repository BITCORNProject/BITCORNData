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
            /*
            var txUser = new TxUser();
            txUser.Amount = 1;
            txUser.Id = _configuration["Config:TestUserId"];
            var txUserList = new List<TxUser>();

            int rainCount = 10;
            for (int i = 0; i < rainCount; i++)
            {
                txUserList.Add(txUser);
            }

            //TODO: this test is failing because tx analytics is not using injected db context
            using (var dbContext = TestUtils.CreateDatabase())
            {
                var startUser = await dbContext.TwitchAsync(txUser.Id);
                var startBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;

                var txController = new TxController(dbContext);
                await txController.Rain(txUserList);

                var endBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                Assert.True(startBalance + (txUser.Amount*rainCount) == endBalance);
            }*/
        }
        [Fact]
        public async Task TestPayoutSuccess()
        {
            /*
            var txUser = new TxUser();
            txUser.Amount = 1;
            txUser.Id = _configuration["Config:TestUserId"];
            var txUserList = new List<TxUser>();

            int payoutCount = 10;
            for (int i = 0; i < payoutCount; i++)
            {
                txUserList.Add(txUser);
            }
            //TODO: this test is failing because tx analytics is not using injected db context
            using (var dbContext = TestUtils.CreateDatabase())
            {
                var startUser = await dbContext.TwitchAsync(txUser.Id);
                var startBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;

                var txController = new TxController(dbContext);
                await txController.Rain(txUserList);

                var endBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                Assert.True(startBalance + (txUser.Amount * payoutCount) == endBalance);
            }*/
        }
        async Task<TipTestResult> Tip(decimal amount, User fromUser, User toUser,bool writeOutput = true)
        {
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                
                var tipResult = new TipTestResult();
                tipResult.TipAmount = amount;

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
                    var fromEnd = dbContext.TwitchAsync(fromUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;//(decimal)responseData[0].From[nameof(UserWallet.Balance).ToLower()];
                    var toEnd = dbContext.TwitchAsync(toUser.UserIdentity.TwitchId).FirstOrDefault().UserWallet.Balance;

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
            var result = new TipTestResult();

            using (var dbContext = TestUtils.CreateDatabase())
            {
                startFromUser = dbContext.TwitchAsync(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                startToUser = dbContext.TwitchAsync(_configuration["Config:TestToUserId"]).FirstOrDefault();
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
           
            result.TipAmount = amount*success;
            using (var dbContext2 = TestUtils.CreateDatabase())
            {
                var endFromUser = dbContext2.TwitchAsync(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var endToUser = dbContext2.TwitchAsync(_configuration["Config:TestToUserId"]).FirstOrDefault();

                result.FromEndBalance = endFromUser.UserWallet.Balance.Value;
                result.ToEndBalance = endToUser.UserWallet.Balance.Value;
            }

            Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.TipAmount);
            Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.TipAmount);
        }

        [Fact]
        public async Task TestTipCornSuccess()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var fromUser = dbContext.TwitchAsync(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchAsync(_configuration["Config:TestToUserId"]).FirstOrDefault();

                var result = await Tip(100, fromUser, toUser);
                Assert.Equal(result.FromEndBalance, result.FromStartBalance - result.TipAmount);
                Assert.Equal(result.ToEndBalance, result.ToStartBalance + result.TipAmount);
               
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
                var fromUser = dbContext.TwitchAsync(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchAsync(_configuration["Config:TestToUserId"]).FirstOrDefault();

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
                var fromUser = dbContext.TwitchAsync(_configuration["Config:TestFromUserId"]).FirstOrDefault();
                var toUser = dbContext.TwitchAsync(_configuration["Config:TestToUserId"]).FirstOrDefault();

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
