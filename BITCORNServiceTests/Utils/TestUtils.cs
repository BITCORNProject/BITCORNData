using BITCORNService;
using BITCORNService.Controllers;
using BITCORNService.Models;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BITCORNService.Utils;

namespace BITCORNServiceTests.Utils
{
    public static class TestUtils
    {
        public static BitcornContext CreateInMemoryDatabase()
        {
           var dbOptions = new DbContextOptionsBuilder<BitcornContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options; 

            return new BitcornContext(dbOptions);
          
        }

        public static IConfigurationRoot GetConfig()
        {
            return new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }

        public static BitcornContext CreateDatabase()
        {
            var dbOptions = new DbContextOptionsBuilder<BitcornContext>()
               .UseSqlServer(GetConfig()["Config:ConnectionString"])
               .EnableSensitiveDataLogging()
               .Options;
          
            return new BitcornContext(dbOptions);

        }

        public static void GetBalances(User testUser, out decimal? downloadBalance, out decimal? referralBalance)
        {
            using (var dbContext = TestUtils.CreateDatabase())
            {
                downloadBalance = dbContext.JoinUserModels().Where(u => u.UserId == testUser.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                referralBalance = dbContext.JoinUserModels().Where(u => u.UserId == 2081).Select(u => u.UserWallet.Balance).FirstOrDefault();
            }
        }

        

        public static void TryRemoveTestUser(string testId)
        {
            var dbContext2 = TestUtils.CreateDatabase();
            try
            {
                var testUser = dbContext2.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testId);
                RemoveUserIfExists(dbContext2,testUser);

            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                dbContext2.Dispose();
            }
        }

        public static void RemoveOldDownloads(string ip)
        {
            using (var dbContext = TestUtils.CreateDatabase())
            {
                dbContext.WalletDownload.RemoveRange(dbContext.WalletDownload.Where(w => w.IPAddress == ip).ToArray());
                dbContext.SaveChanges();
            }
        }

        public static WalletDownload CreateDownload(string ip, DateTime time, User testUser,int referralUserId = 2081)
        {
            return new WalletDownload
            {
                IPAddress = ip,
                Country = "middle earth",
                ReferralCode = "2",
                UserId = testUser.UserId,
                TimeStamp = time,
                WalletVersion = "3.1.0",
                ReferralUserId = referralUserId,
                ReferralId = 0,
                Platform = "windows"
            };
        }

        public static void TryRemoveTestTwitchUser()
        {
            using (var dbContext = TestUtils.CreateDatabase())
            {
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.TwitchId == "1337");
                RemoveUserIfExists(dbContext, user);
                
            }
        }

        public static User CreateTestUser(string testName, string testAuth0Id, int refId = 0)
        {
            var user = RegisterController.CreateUser(new Auth0User()
            {
                Auth0Id = testAuth0Id,
                Auth0Nickname = testName
            }, refId);

            TryRemoveTestUser(testAuth0Id);

            using (var dbContext = TestUtils.CreateDatabase())
            {
                dbContext.User.Add(user);
                dbContext.SaveChanges();
                return user;
            }
        }

        public static User CreateTestUserWithoutRef(string testName, string testAuth0Id)
        {
            TryRemoveTestUser(testAuth0Id);
            var user = new User
            {
                UserIdentity = new UserIdentity
                {
                    Auth0Id = testAuth0Id,
                    Auth0Nickname = testName
                },
                UserWallet = new UserWallet(),
                UserStat = new UserStat()
            };



            using (var dbContext = TestUtils.CreateDatabase())
            {
                try
                {
                    dbContext.User.Add(user);
                    int rows = dbContext.SaveChanges();
                    return user;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    throw e;
                }
            }
        }
        public static void AddUser(this BitcornContext context, UserIdentity identity,UserWallet wallet)
        {
            var user = new User();
            user.UserWallet = wallet;
            user.UserIdentity = identity;
            user.UserStat = new UserStat();
            context.User.Add(user);

            context.SaveChanges();

        }

        public static void RemoveUserIfExists(BitcornContext dbContext,User user)
        {
            if (user != null)
            {
                dbContext.Database.ExecuteSqlRaw("delete socialidentity");
                dbContext.Database.ExecuteSqlRaw("delete subtx");
                dbContext.Database.ExecuteSqlRaw("delete corndeposit");

                var referral = dbContext.UserReferral.FirstOrDefault(u => u.UserId == user.UserId);
                
                if(referral!=null)
                    dbContext.UserReferral.Remove(referral);
                
                dbContext.UserSubscription.RemoveRange(dbContext.UserSubscription.Where(u=>u.UserId==user.UserId).ToArray());
                dbContext.WalletDownload.RemoveRange(dbContext.WalletDownload.Where(w=>w.UserId==user.UserId).ToArray());
                dbContext.Remove(user.UserWallet);
                dbContext.Remove(user.UserIdentity);
                dbContext.Remove(user.UserStat);
                dbContext.Remove(user);
                dbContext.SaveChanges();
            }
        }
        public static async Task RegisterNewUserWithReferralArgs(string testName, string testId, int referralId)
        {
            TestUtils.TryRemoveTestUser(testId);

            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var referrer = dbContext.Referrer.FirstOrDefault(u => u.ReferralId == referralId);
                var startReferrerBal = dbContext.JoinUserModels().Where(u => u.UserId == referrer.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();

                var response = await RegisterNewUserWithReferralInternal(testName, testId, referralId.ToString());
                var wallet = dbContext.UserWallet.FirstOrDefault(u => u.UserId == response.Value.UserId);
                Assert.Equal(referrer.Amount, wallet.Balance);

                var dbContext2 = TestUtils.CreateDatabase();
                try
                {
                    var endReferrerBal = dbContext2.JoinUserModels().Where(u => u.UserId == referrer.UserId).Select(u => u.UserWallet.Balance).FirstOrDefault();
                    var referralPayoutTotal = await ReferralUtils.TotalReward(dbContext2, referrer);
                    Assert.Equal(startReferrerBal + referralPayoutTotal, endReferrerBal);
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

        public static async Task<ActionResult<FullUser>> RegisterNewUserWithReferralInternal(string testName, string testId, string referralId)
        {
            var dbContext = TestUtils.CreateDatabase();

            try
            {
                var controller = new RegisterController(dbContext, TestUtils.GetConfig());
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

        public static async Task TestWalletDownloadWithReferrerInternal(BitcornContext dbContext, string ip, User testUser, bool shouldCompleteBonus)
        {


            TestUtils.GetBalances(testUser, out decimal? downloadStartBalance, out decimal? referralStartBalance);
            var userReferral = dbContext.UserReferral.FirstOrDefault(u => u.UserId == testUser.UserId);
            var referrer = dbContext.Referrer.FirstOrDefault(u => u.ReferralId == userReferral.ReferralId);
            var controller = new WalletDownloadController(dbContext);
            var res = await controller.Download(TestUtils.CreateDownload(ip, DateTime.Now, testUser, referrer.UserId), DateTime.Now);

            Assert.Equal(200, (res as StatusCodeResult).StatusCode);

            var dbContext2 = TestUtils.CreateDatabase();
            try
            {
                var referralPayoutTotal = await ReferralUtils.TotalReward(dbContext, referrer) + await ReferralUtils.WalletBonusReward(dbContext, referrer, 10); ;

                TestUtils.GetBalances(testUser, out decimal? downloadEndBalance, out decimal? referralEndBalance);
                decimal bonus = 0;
                if (shouldCompleteBonus)
                {
                    bonus = ReferralUtils.BONUS_PAYOUT;
                }
                Assert.Equal(referralStartBalance + referralPayoutTotal + bonus, referralEndBalance);
                Assert.Equal(downloadStartBalance + 10 + referrer.Amount + bonus, downloadEndBalance);

            }
            finally
            {
                dbContext2.Dispose();
            }

        }

        public static async Task<PlatformSyncResponse> SyncPlatform(string testAuth0Id, string platform, string platformId = "1337")
        {
            var dbContext = TestUtils.CreateDatabase();

            var controller = new RegisterController(dbContext, TestUtils.GetConfig());
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
