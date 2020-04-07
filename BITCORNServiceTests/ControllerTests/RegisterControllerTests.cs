using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils;
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
            await TestUtils.SyncPlatform(testId, "discord");
        }

        [Fact]
        public async Task TestSyncWithoutRef()
        {
            var testName = "test";
            var testId = "auth0|test";
            TestUtils.TryRemoveTestTwitchUser();
            TestUtils.CreateTestUserWithoutRef(testName, testId);
            using (var db = TestUtils.CreateDatabase())
            {
                var u = db.UserIdentity.Where(u => u.Auth0Id == testId).FirstOrDefault();
            }
            await TestUtils.SyncPlatform(testId, "twitch");
        }

        [Fact]
        public async Task TestSyncTwitter()
        {
            var testName = "test";
            var testId = "auth0|test";

            TestUtils.CreateTestUser(testName, testId);
            await TestUtils.SyncPlatform(testId, "twitter");
        }

        [Fact]
        public async Task TestSyncReddit()
        {
            var testName = "test";
            var testId = "auth0|test";

            TestUtils.CreateTestUser(testName, testId);
            await TestUtils.SyncPlatform(testId, "reddit");
        }

        [Fact]
        public async Task TestSyncTwitch()
        {

            var testName = "test";
            var testId = "auth0|test";
            TestUtils.TryRemoveTestTwitchUser();
            TestUtils.CreateTestUser(testName, testId);
            await TestUtils.SyncPlatform(testId, "twitch");
        }

        [Fact]
        public async Task TestUserMigration()
        {
            TestUtils.TryRemoveTestTwitchUser();
            var testName = "test";
            var testId = "auth0|test";
            var testUser = TestUtils.CreateTestUser(testName, testId);
            int oldProfileId = testUser.UserId;
            var twitchId = "1337";

            await TestUtils.SyncPlatform(testId, "twitch", twitchId);
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testUser.UserIdentity.Auth0Id);
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

                var res = await TestUtils.SyncPlatform(profile2Auth0Id, "twitch", twitchId);
                Assert.True(res.IsMigration);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var oldUser = dbContext2.JoinUserModels().FirstOrDefault(u => u.UserId == testUser.UserId);
                    TestUtils.RemoveUserIfExists(dbContext2, oldUser);
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
        public static async Task TestSyncPlatformWithReferral()
        {
            var testName = "test";
            var testId = "auth0|test";

            TestUtils.TryRemoveTestUser(testId);
            TestUtils.TryRemoveTestTwitchUser();
            await TestSyncPlatformWithReferralInternal(testName, testId, true);
            TestUtils.TryRemoveTestUser(testId);
            TestUtils.TryRemoveTestTwitchUser();
            using (var db = TestUtils.CreateDatabase())

            {
                db.SocialIdentity.Add(new SocialIdentity()
                {
                    PlatformId = "twitch|1337",
                    Timestamp = DateTime.Now
                });
                db.SaveChanges();
            }
            await TestSyncPlatformWithReferralInternal(testName, testId, false);
        }

        static async Task TestSyncPlatformWithReferralInternal(string testName, string testId, bool expectSuccess, bool shouldCreateNewUser = true, int referrerId = 2)
        {

            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var referrer = dbContext.Referrer.FirstOrDefault(u => u.ReferralId == referrerId);
                var startReferrerBal = dbContext.JoinUserModels().Where(u => u.UserId == referrer.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();

                await TestUtils.RegisterNewUserWithReferralInternal(testName, testId, referrerId.ToString());
                var userStartBalance = dbContext.JoinUserModels()
                    .Where(u => u.UserIdentity.Auth0Id == testId).Select(u => u.UserWallet.Balance).FirstOrDefault();

                var response = await TestUtils.SyncPlatform(testId, "twitch");


                var dbContext2 = TestUtils.CreateDatabase();
                try
                {

                    var wallet = dbContext2.UserWallet.FirstOrDefault(u => u.UserId == response.User.UserId);


                    var endReferrerBal = dbContext2.JoinUserModels().Where(u => u.UserId == referrer.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    var referralPayoutTotal = await ReferralUtils.TotalReward(dbContext2, referrer);

                    if (expectSuccess)
                    {
                        Assert.Equal(referrer.Amount + userStartBalance, wallet.Balance);
                        if (shouldCreateNewUser)
                            referralPayoutTotal *= 2;

                        Assert.Equal(startReferrerBal + referralPayoutTotal, endReferrerBal);
                    }
                    else
                    {
                        Assert.Equal(referrer.Amount, wallet.Balance);
                        decimal plus = 0;
                        if (shouldCreateNewUser)
                            plus += referralPayoutTotal;

                        Assert.Equal(startReferrerBal + plus, endReferrerBal);
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

            await TestUtils.RegisterNewUserWithReferralArgs(testName, testId, 2);
        }

        [Fact]
        public async Task TestFullReferralWithoutSub()
        {
            await RunFullReferralTest(false);   
        }

        [Fact]
        public async Task TestFullReferralWithSub()
        {
            await RunFullReferralTest(true);
        }

        [Fact]
        public async Task TestFullReferralWithSubAsLastCompletion()
        {
            await RunFullReferralTest(true,false);
        }

        async Task RunFullReferralTest(bool sub,bool runSubFirst = true)
        {
            var testName = "test";
            var testId = "auth0|test";
            await TestUtils.RegisterNewUserWithReferralArgs(testName, testId, 2);
            await TestSyncPlatformWithReferralInternal(testName, testId, true, false, 2);
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testId);
                user.UserWallet.Balance += UserWalletController.MIN_BALANCE_QUEST_AMOUNT;
                dbContext.SaveChanges();

                var userWalletController = new UserWalletController(dbContext);
                await userWalletController.Wallet("twitch|1337");
                if (sub&&runSubFirst)
                {
                    var resp = await SubscriptionUtils.Subscribe(dbContext,user,new SubRequest() { 
                        SubscriptionName = "BITCORNFarms",
                        Tier = 1,
                        Platform = "test",
                        Amount= 1000
                    });
                }
                await TestUtils.TestWalletDownloadWithReferrerInternal(dbContext, "1.1.1.1", user, sub&&runSubFirst);

                if (sub && !runSubFirst)
                {
                    var resp = await SubscriptionUtils.Subscribe(dbContext, user, new SubRequest()
                    {
                        SubscriptionName = "BITCORNFarms",
                        Tier = 1,
                        Platform = "test",
                        Amount = 1000
                    });
                }
                var userReferral = dbContext.UserReferral.FirstOrDefault(u => u.UserId == user.UserId);

                Assert.True(userReferral.SignupReward != null);
                Assert.True(userReferral.MinimumBalanceDate != null);
                Assert.True(userReferral.WalletDownloadDate != null);
                Assert.True(userReferral.SyncDate != null);
                
                if (!sub)
                {
                    Assert.True(userReferral.Bonus == null);
                    Assert.True(userReferral.ReferrerBonus == null);
                    Assert.True(userReferral.UserSubscriptionId == null);
                }
                else
                {
                    Assert.True(userReferral.UserSubscriptionId != null);
                    Assert.True(userReferral.Bonus != null);
                    Assert.True(userReferral.ReferrerBonus != null);
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
        
    }
}
