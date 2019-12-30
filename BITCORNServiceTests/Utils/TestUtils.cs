using BITCORNService;
using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
               .Options;

            return new BitcornContext(dbOptions);

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
