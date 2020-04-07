using BITCORNService.Utils;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNServiceTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BITCORNServiceTests
{
    public class SubscriptionTests
    {
        [Fact]
        public async Task TestNotSubbedCheck()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var testName = "test";
                var testId = "auth0|test";

                TestUtils.TryRemoveTestUser(testId);

                TestUtils.CreateTestUser(testName, testId);
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testId);
                
                Assert.False(await SubscriptionUtils.HasSubscribed(dbContext, user, "BITCORNFarms", 1));
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestSubscription()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var testName = "test";
                var testId = "auth0|test";

                TestUtils.TryRemoveTestUser(testId);

                TestUtils.CreateTestUser(testName, testId);
                var user = dbContext.JoinUserModels().FirstOrDefault(u=>u.UserIdentity.Auth0Id==testId);
                user.UserWallet.Balance += 1001;
                dbContext.SaveChanges();
                var resp = await SubscriptionUtils.Subscribe(dbContext, user, new SubRequest()
                {
                    SubscriptionName = "BITCORNFarms",
                    Tier = 1,
                    Platform = "test",
                    Amount = 1000
                });

                Assert.True(await SubscriptionUtils.HasSubscribed(dbContext,user, "BITCORNFarms",1));
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestSubscriptionTierCheck()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var testName = "test";
                var testId = "auth0|test";

                TestUtils.TryRemoveTestUser(testId);

                TestUtils.CreateTestUser(testName, testId);
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testId);
                user.UserWallet.Balance += 1001;
                dbContext.SaveChanges();
                var resp = await SubscriptionUtils.Subscribe(dbContext, user, new SubRequest()
                {
                    SubscriptionName = "BITCORNFarms",
                    Tier = 1,
                    Platform = "test",
                    Amount = 1000
                });

                Assert.False(await SubscriptionUtils.HasSubscribed(dbContext, user, "BITCORNFarms", 2));
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestSubscriptionInsufficientFunds()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var testName = "test";
                var testId = "auth0|test";

                TestUtils.TryRemoveTestUser(testId);

                TestUtils.CreateTestUser(testName, testId);
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testId);
                
                var resp = await SubscriptionUtils.Subscribe(dbContext, user, new SubRequest()
                {
                    SubscriptionName = "BITCORNFarms",
                    Tier = 1,
                    Platform = "test",
                    Amount = 1000
                });

                Assert.False(await SubscriptionUtils.HasSubscribed(dbContext, user, "BITCORNFarms", 1));
            }
            finally
            {
                dbContext.Dispose();
            }
        }

        [Fact]
        public async Task TestSubscriptionExpired()
        {
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var testName = "test";
                var testId = "auth0|test";

                TestUtils.TryRemoveTestUser(testId);

                TestUtils.CreateTestUser(testName, testId);
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testId);
                user.UserWallet.Balance += 1001;
                dbContext.SaveChanges();
                var resp = await SubscriptionUtils.Subscribe(dbContext, user, new SubRequest()
                {
                    SubscriptionName = "BITCORNFarms",
                    Tier = 1,
                    Platform = "test",
                    Amount = 1000
                });

                var userSubscription = dbContext.UserSubscription.FirstOrDefault(u=>u.UserId==user.UserId);
                userSubscription.LastSubDate = DateTime.Now.AddDays(-31);
                dbContext.Update(userSubscription);
                var changes = dbContext.SaveChanges();

                Assert.False(await SubscriptionUtils.HasSubscribed(dbContext, user, "BITCORNFarms", 1));
            }
            finally
            {
                dbContext.Dispose();
            }
        }
    }
}
