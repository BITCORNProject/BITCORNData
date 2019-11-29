using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNServiceTests.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BITCORNServiceTests
{
    public class TxControllerTests
    {
        private IConfigurationRoot _configuration;
        public TxControllerTests()
        {
            _configuration = TestUtilities.GetConfig();
        }
        [Fact]
        public async Task TestRainSuccess()
        {
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
            using (var dbContext = TestUtilities.CreateDatabase())
            {
                var startUser = await dbContext.TwitchAsync(txUser.Id);
                var startBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;

                var txController = new TxController(dbContext);
                await txController.Rain(txUserList);

                var endBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                Assert.True(startBalance + (txUser.Amount*rainCount) == endBalance);
            }
        }
        [Fact]
        public async Task TestPayoutSuccess()
        {
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
            using (var dbContext = TestUtilities.CreateDatabase())
            {
                var startUser = await dbContext.TwitchAsync(txUser.Id);
                var startBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;

                var txController = new TxController(dbContext);
                await txController.Rain(txUserList);

                var endBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                Assert.True(startBalance + (txUser.Amount * payoutCount) == endBalance);
            }
        }
        [Fact]
        public async Task TestTipCornSuccess()
        {
            var txUser = new TxUser();
            txUser.Amount = 1;
            txUser.Id = _configuration["Config:TestUserId"]; 

            //TODO: this test is failing because tx analytics is not using injected db context
            using (var dbContext = TestUtilities.CreateDatabase())
            {
                var startUser = await dbContext.TwitchAsync(txUser.Id);
                var startBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;

                var txController = new TxController(dbContext);
                await txController.Tipcorn(txUser);

                var endBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                Assert.True(startBalance + txUser.Amount == endBalance);
            }
        }
        [Fact]
        public async Task TestWithdrawSuccess()
        {
            
            var withdrawUser = new WithdrawUser()
            {
                Id = _configuration["Config:TestUserId"],
                Amount = 1,
                CornAddy = _configuration["Config:TestAddress"]
            };

            using (var dbContext = TestUtilities.CreateDatabase())
            {
                var txController = new TxController(dbContext);

                var startUser = await dbContext.TwitchAsync(withdrawUser.Id);
                var startBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                await txController.Withdraw(withdrawUser);

                var endBalance = dbContext.UserWallet.FirstOrDefault(w => w.UserId == startUser.UserId).Balance;
                Assert.True(startBalance - withdrawUser.Amount == endBalance);
            }
        }
    }
}
