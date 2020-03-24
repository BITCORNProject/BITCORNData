using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNServiceTests.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BITCORNServiceTests.ControllerTests
{
    public class RegisterControllerTests
    {
        private IConfigurationRoot _configuration;
        public RegisterControllerTests()
        {
            _configuration = TestUtils.GetConfig();
        }
        
        [Fact]
        public async Task TestSyncDiscord()
        {
            var testName = "test";
            var testId = "auth0|test";

            TestUtils.CreateTestUser(testName, testId);
            await SyncPlatform(testId, "discord");
        }
        [Fact]
        public async Task TestSyncWithoutRef()
        {
            var testName = "test";
            var testId = "auth0|test";
            TestUtils.TryRemoveTestTwitchUser();
            TestUtils.CreateTestUserWithoutRef(testName, testId);
            using(var db = TestUtils.CreateDatabase())
            {
                var u = db.UserIdentity.Where(u=>u.Auth0Id==testId).FirstOrDefault();
            }
            await SyncPlatform(testId, "twitch");
        }
        [Fact]
        public async Task TestSyncTwitter()
        {
            var testName = "test";
            var testId = "auth0|test";

            TestUtils.CreateTestUser(testName, testId);
            await SyncPlatform(testId, "twitter");
        }
        [Fact]
        public async Task TestSyncReddit()
        {
            var testName = "test";
            var testId = "auth0|test";

            TestUtils.CreateTestUser(testName, testId);
            await SyncPlatform(testId, "reddit");
        }
        [Fact]
        public async Task TestSyncTwitch()
        {

            var testName = "test";
            var testId = "auth0|test";
            TestUtils.TryRemoveTestTwitchUser();
            TestUtils.CreateTestUser(testName,testId);
            await SyncPlatform(testId,"twitch");
        }
        [Fact]
        public async Task TestUserMigration()
        {
            TestUtils.TryRemoveTestTwitchUser();
            var testName = "test";
            var testId = "auth0|test";
            var testUser = TestUtils.CreateTestUser(testName,testId);
            int oldProfileId = testUser.UserId;
            var twitchId = "1337";

            await SyncPlatform(testId, "twitch", twitchId);
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                var user = dbContext.JoinUserModels().FirstOrDefault(u=>u.UserIdentity.Auth0Id==testUser.UserIdentity.Auth0Id);
                user.UserIdentity.Auth0Id = null;
                
                
                dbContext.SaveChanges();

                var controller = new RegisterController(dbContext, _configuration);
                var context = controller.ControllerContext.HttpContext = new DefaultHttpContext();
                var profile2Auth0Id = "auth0|test2";
                TestUtils.TryRemoveTestUser(profile2Auth0Id);
                await controller.RegisterNewUser(new Auth0User() { 
                    Auth0Id = profile2Auth0Id,
                    Auth0Nickname = "test2"
                });

                var res = await SyncPlatform(profile2Auth0Id, "twitch", twitchId);
                Assert.True(res.IsMigration);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var oldUser = dbContext2.JoinUserModels().FirstOrDefault(u=>u.UserId==testUser.UserId);
                    TestUtils.TryRemoveTestUser(oldUser, dbContext2);
                }
                TestUtils.TryRemoveTestUser(profile2Auth0Id);
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestRegisterNewUser()
        {
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                var controller = new RegisterController(dbContext, _configuration);
                var context = controller.ControllerContext.HttpContext = new DefaultHttpContext();
                var testName = "test";
                var testId = "auth0|test";
                TestUtils.TryRemoveTestUser(testId);

                var response = await controller.RegisterNewUser(new Auth0User()
                {
                    Auth0Nickname = testName,
                    Auth0Id = testId
                }, "0");

                Assert.Equal(testName, response.Auth0Nickname);
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        [Fact]
        public async Task TestSyncPlatformWithReferral()
        {
            var testName = "test";
            var testId = "auth0|test";
            TestUtils.TryRemoveTestUser(testId);
            TestUtils.TryRemoveTestTwitchUser();
            await TestSyncPlatformWithReferralInternal(testName,testId, true);
            TestUtils.TryRemoveTestUser(testId);
            TestUtils.TryRemoveTestTwitchUser(false);
            await TestSyncPlatformWithReferralInternal(testName, testId, false);
        }
        async Task TestSyncPlatformWithReferralInternal(string testName,string testId,bool expectSuccess)
        {

            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var referrer = dbContext.Referrer.FirstOrDefault(u => u.ReferralId == 2);
                var startReferrerBal = dbContext.JoinUserModels().Where(u => u.UserId == referrer.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();

                await TestRegisterNewUserWithReferralInternal(testName, testId, "2");
                var response = await SyncPlatform(testId, "twitch");


                var dbContext2 = TestUtils.CreateDatabase();
                try
                {

                    var wallet = dbContext2.UserWallet.FirstOrDefault(u => u.UserId == response.User.UserId);
                   

                    var endReferrerBal = dbContext2.JoinUserModels().Where(u => u.UserId == referrer.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    if (expectSuccess)
                    {
                        Assert.Equal(referrer.Amount * 2, wallet.Balance);
                        Assert.Equal(startReferrerBal + referrer.Amount * 2, endReferrerBal);
                    }
                    else
                    {
                        Assert.Equal(referrer.Amount, wallet.Balance);
                        Assert.Equal(startReferrerBal + referrer.Amount, endReferrerBal);
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
        [Fact]
        public async Task TestRegisterNewUserWithReferral()
        {
            var testName = "test";
            var testId = "auth0|test";
            TestUtils.TryRemoveTestUser(testId);
           
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var referrer = dbContext.Referrer.FirstOrDefault(u=>u.ReferralId==2);
                var startReferrerBal = dbContext.JoinUserModels().Where(u=>u.UserId==referrer.UserId).Select(u=>u.UserWallet.Balance).FirstOrDefault();

                var response = await TestRegisterNewUserWithReferralInternal(testName, testId, "2");
                var wallet = dbContext.UserWallet.FirstOrDefault(u => u.UserId == response.UserId);
                Assert.Equal(referrer.Amount, wallet.Balance);

                var dbContext2 = TestUtils.CreateDatabase();
                try
                {
                    var endReferrerBal = dbContext2.JoinUserModels().Where(u => u.UserId == referrer.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    Assert.Equal(startReferrerBal+referrer.Amount,endReferrerBal);
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
        async Task<FullUser> TestRegisterNewUserWithReferralInternal(string testName,string testId,string referralId)
        {
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                var controller = new RegisterController(dbContext, _configuration);
                var context = controller.ControllerContext.HttpContext = new DefaultHttpContext();
                
               // string referreralId = "2";

                return await controller.RegisterNewUser(new Auth0User()
                {
                    Auth0Nickname = testName,
                    Auth0Id = testId
                }, referralId);
            }
            finally
            {
                dbContext.Dispose();
            }
        }
      
        async Task<PlatformSyncResponse> SyncPlatform(string testAuth0Id,string platform,string platformId = "1337")
        {
            var dbContext = TestUtils.CreateDatabase();

            var controller = new RegisterController(dbContext, _configuration);
            var context = controller.ControllerContext.HttpContext = new DefaultHttpContext();
            var testPlatformId = $"{platform}|{platformId}";

            var res = await controller.Register(new RegistrationData()
            {
                Auth0Id = testAuth0Id,
                PlatformId = testPlatformId
            });

            var outputId = string.Empty;
            if (platform == "twitch") outputId = res.User.TwitchId;
            else if (platform == "discord") outputId = res.User.DiscordId;
            else if (platform == "twitter") outputId = res.User.TwitterId;
            else if (platform == "reddit") outputId = res.User.RedditId;
            else throw new ArgumentException("platform");

            Assert.Equal(testPlatformId.Split('|')[1], outputId);
            return res;
        }

    }
}
