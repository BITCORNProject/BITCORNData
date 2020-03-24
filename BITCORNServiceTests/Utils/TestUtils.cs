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
        public static void TryRemoveTestUser(User testUser, BitcornContext dbContext)
        {
            if (testUser != null)
            {
                dbContext.Remove(testUser.UserIdentity);
                dbContext.Remove(testUser.UserWallet);
                dbContext.Remove(testUser.UserStat);
                dbContext.Remove(testUser);
                dbContext.SaveChanges();
            }
        }
        public static void TryRemoveTestUser(string testId)
        {
            var dbContext2 = TestUtils.CreateDatabase();
            try
            {
                var testUser = dbContext2.JoinUserModels().FirstOrDefault(u => u.UserIdentity.Auth0Id == testId);
                TryRemoveTestUser(testUser, dbContext2);

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
        public static void TryRemoveTestTwitchUser(bool removeSocialIdentity = true)
        {
            using (var dbContext = TestUtils.CreateDatabase())
            {
                if (removeSocialIdentity)
                {
                    var socialIdentity = dbContext.SocialIdentity.FirstOrDefault(u => u.PlatformId == "twitch|1337");
                    if (socialIdentity != null)
                    {
                        dbContext.Remove(socialIdentity);
                        dbContext.SaveChanges();
                    }
                }
                var user = dbContext.JoinUserModels().FirstOrDefault(u => u.UserIdentity.TwitchId == "1337");
                if (user != null)
                {
                    TryRemoveTestUser(user, dbContext);
                }
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

        public static void RemoveUserIfExists(DbContext dbContext,User user)
        {
            if (user != null)
            {
                dbContext.Remove(user.UserWallet);
                dbContext.Remove(user.UserIdentity);
                dbContext.Remove(user.UserStat);
                dbContext.Remove(user);
                dbContext.SaveChanges();
            }
        }
    }
}
