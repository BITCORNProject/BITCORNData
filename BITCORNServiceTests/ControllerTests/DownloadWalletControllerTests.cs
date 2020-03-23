using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNServiceTests.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BITCORNServiceTests.ControllerTests
{
    public class DownloadWalletControllerTests
    {
        private IConfigurationRoot _configuration;
        public DownloadWalletControllerTests()
        {
            _configuration = TestUtils.GetConfig();
        }
        [Fact]
        public async Task TestWalletDownloadWithReferral()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var ip = "1.1.1.1";
                RemoveOldDownloads(ip);
                var testUser = TestUtils.CreateTestUser("test","auth0|test",2);
                
                var downloadStartBalance = dbContext.JoinUserModels().Where(u=>u.UserId == testUser.UserId).Select(u=>u.UserWallet.Balance).FirstOrDefault();
                var referralStartBalance = dbContext.JoinUserModels().Where(u => u.UserId == 2081).Select(u => u.UserWallet.Balance).FirstOrDefault();
                
                var controller = new WalletDownloadController(dbContext);
                var res = await controller.Download(CreateDownload(ip, DateTime.Now, testUser), DateTime.Now);

                Assert.Equal(200,(res as StatusCodeResult).StatusCode);

                var dbContext2 = TestUtils.CreateDatabase();
                try
                {
                    var downloadEndBalance = dbContext2.JoinUserModels().Where(u => u.UserId == testUser.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    var referralEndBalance = dbContext2.JoinUserModels().Where(u => u.UserId == 2081).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    Assert.True(downloadEndBalance > downloadStartBalance);
                    Assert.True(referralEndBalance > referralStartBalance);

                }
                finally
                {
                    dbContext2.Dispose();
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestWalletDownloadRewards()
        {
            await TestMultiWalletDownload(DateTime.Now,false);
            await TestMultiWalletDownload(DateTime.Now.AddDays(8), true);

        }
        [Fact]
        public async Task TestWalletDownloadWithoutReferral()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var ip = "1.1.1.1";
                RemoveOldDownloads(ip);
                var testUser = TestUtils.CreateTestUserWithoutRef("test", "auth0|test");
 
                var downloadStartBalance = dbContext.JoinUserModels().Where(u => u.UserId == testUser.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                var referralStartBalance = dbContext.JoinUserModels().Where(u => u.UserId == 2081).Select(u => u.UserWallet.Balance).FirstOrDefault();

                var controller = new WalletDownloadController(dbContext);
                var res = await controller.Download(CreateDownload(ip,DateTime.Now,testUser), DateTime.Now);

                Assert.Equal(200, (res as StatusCodeResult).StatusCode);
                var dbContext2 = TestUtils.CreateDatabase();

                try
                {
                    var downloadEndBalance = dbContext2.JoinUserModels().Where(u => u.UserId == testUser.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    var referralEndBalance = dbContext2.JoinUserModels().Where(u => u.UserId == 2081).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    Assert.Equal(downloadEndBalance, downloadStartBalance);
                    Assert.Equal(referralEndBalance, referralStartBalance);

                }
                finally
                {
                    dbContext2.Dispose();
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        async Task TestMultiWalletDownload(DateTime otherTime, bool expectSuccess)
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var controller = new WalletDownloadController(dbContext);

                var ip = "1.1.1.1";
                RemoveOldDownloads(ip);

                var testUser = TestUtils.CreateTestUser("test", "auth0|test");

                await controller.Download(CreateDownload(ip, DateTime.Now, testUser), DateTime.Now);
                var downloadStartBalance = dbContext.JoinUserModels().Where(u => u.UserId == testUser.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                var referralStartBalance = dbContext.JoinUserModels().Where(u => u.UserId == 2081).Select(u => u.UserWallet.Balance).FirstOrDefault();

                var res = await controller.Download(CreateDownload(ip, otherTime, testUser), otherTime);

                Assert.Equal(200, (res as StatusCodeResult).StatusCode);

                var dbContext2 = TestUtils.CreateDatabase();

                try
                {
                    var downloadEndBalance = dbContext2.JoinUserModels().Where(u => u.UserId == testUser.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    var referralEndBalance = dbContext2.JoinUserModels().Where(u => u.UserId == 2081).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    if (!expectSuccess)
                    {
                        Assert.Equal(downloadEndBalance, downloadStartBalance);
                        Assert.Equal(referralEndBalance, referralStartBalance);
                    }
                    else
                    {
                        Assert.True(downloadEndBalance > downloadStartBalance);
                        Assert.True(referralEndBalance > referralStartBalance);
                    }
                }
                finally
                {
                    dbContext2.Dispose();
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        WalletDownload CreateDownload(string ip,DateTime time,User testUser)
        {
            return new WalletDownload
            {
                IPAddress = ip,
                Country = "middle earth",
                ReferralCode = "2",
                UserId = testUser.UserId,
                TimeStamp = time,
                WalletVersion = "3.1.0",
                ReferralUserId = 2081,
                ReferralId = 0,
                Platform = "windows"
            };
        }
        void RemoveOldDownloads(string ip)
        {
            using (var dbContext = TestUtils.CreateDatabase())
            {
                dbContext.WalletDownload.RemoveRange(dbContext.WalletDownload.Where(w => w.IPAddress == ip).ToArray());
                dbContext.SaveChanges();
            }
        }
    }
}
