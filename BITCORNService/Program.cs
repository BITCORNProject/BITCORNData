using BITCORNService.Models;
using BITCORNService.Platforms;
using BITCORNService.Utils;
using BITCORNService.Utils.Models;
using BITCORNService.Utils.Twitch;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BITCORNService
{
    public class Program
    {
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

        public static async Task<UserIdentity> GetUserIdentity(string twitchId)
        {
            var dbContext = CreateDatabase();
            var streamQuery = await dbContext.UserIdentity
                .Where(x => x.TwitchId == twitchId).FirstOrDefaultAsync();

            return streamQuery;
        }

        public static async Task Main(string[] args)
        {

            var d = DateTime.Parse("2013-06-03T19:12:02.580593Z");

            IConfiguration config = GetConfig();

            BitcornContext dbContext = CreateDatabase();

            string clientId = config.GetSection("Config").GetSection("TwitchClientIdSub").Value;
            UserIdentity userIdentity = await GetUserIdentity("75987197");
            string twitchAccessToken = await TwitchPlatform.RefreshToken(dbContext, userIdentity, config);

            var twitchUser = TwitchHelix.GetTwitchUser(clientId, userIdentity.TwitchId, twitchAccessToken);
            Console.WriteLine(twitchUser);

            var helix = new Helix(config, dbContext, twitchAccessToken);

            CreateHostBuilder(args).Build().Run();
        }

        private static Task GetTwitchUser(IConfiguration config, string twitchId, string twitchAccessToken)
        {
            throw new NotImplementedException();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseSentry();
                });

    }
}
