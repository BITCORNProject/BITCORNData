using BITCORNService.Controllers;
using BITCORNService.Utils.DbActions;
using BITCORNService.Utils.Models;
using BITCORNServiceTests.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BITCORNServiceTests
{
    public class SubUpdateTest
    {

        [Fact]
        public async Task TestSubUpdate()
        {
            var config = TestUtils.GetConfig();
            var dbContext = TestUtils.CreateDatabase();
            try
            {
                var twitchIds = new string[] {
                    config["Config:TestFromUserId"],
                    config["TestToUserId"]
                };

                await dbContext.Database.ExecuteSqlRawAsync("UPDATE [user] SET [subtier] = 0");
                var subs = new Sub[]{
                    new Sub()
                    {
                        TwitchId = config["Config:TestFromUserId"],
                        Tier = "3000",
                    },
                    new Sub()
                    {
                        TwitchId = config["Config:TestToUserId"],
                        Tier = "3000",
                    }
                };
                var userController = new UserController(TestUtils.GetConfig(),dbContext);
                await userController.Post(subs);
                using (var dbContext2 = TestUtils.CreateDatabase())
                {
                    var users2 = dbContext2.JoinUserModels().Where(u => twitchIds.Contains(u.UserIdentity.TwitchId));
                    foreach (var user in users2.ToArray())
                    {
                        Assert.Equal(3, user.SubTier);
                    }
                    await dbContext2.Database.ExecuteSqlRawAsync("UPDATE [user] SET [subtier] = 0");
                }
            }
            finally
            {
                dbContext.Dispose();
            }
        }
    }
}
