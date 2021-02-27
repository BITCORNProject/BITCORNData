using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using BITCORNService.Games.Models;

using BITCORNService.Models;
using BITCORNService.Utils.Auth;
using BITCORNService.Utils.LockUser;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BITCORNService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
          
            var connection = Configuration["Config:ConnectionString"];
            var auth0Domain = $"https://{Configuration["Config:Auth0:Domain"]}/";
            var audience = Configuration["Config:Auth0:ApiIdentifier"];
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = auth0Domain;
                options.Audience = audience;
            });

            services.AddAuthorization(options =>
            {

                options.AddPolicy(AuthScopes.SendTransaction,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.SendTransaction, auth0Domain)));

                options.AddPolicy(AuthScopes.ReadTransaction,
                   policy => policy.Requirements.Add(new RequireScope(AuthScopes.ReadTransaction, auth0Domain)));

                options.AddPolicy(AuthScopes.Deposit,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.Deposit, auth0Domain)));

                options.AddPolicy(AuthScopes.Withdraw,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.Withdraw, auth0Domain)));

                options.AddPolicy(AuthScopes.ChangeUser,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.ChangeUser, auth0Domain)));

                options.AddPolicy(AuthScopes.AddUser,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.AddUser, auth0Domain)));

                options.AddPolicy(AuthScopes.BanUser,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.BanUser, auth0Domain)));

                options.AddPolicy(AuthScopes.ReadUser,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.ReadUser, auth0Domain)));

                options.AddPolicy(AuthScopes.CreateOrder,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.CreateOrder, auth0Domain)));

                options.AddPolicy(AuthScopes.AuthorizeOrder,
                    policy => policy.Requirements.Add(new RequireScope(AuthScopes.AuthorizeOrder, auth0Domain)));
            });


            services.AddSingleton<IAuthorizationHandler, RequireScopeHandler>();
            services.AddScoped<LockUserAttribute>();
            services.AddScoped<CacheUserAttribute>();
            services.AddSingleton(Configuration);

         
            services.AddDbContext<BitcornContext>(options =>
                options.UseSqlServer(connection,
                Options => Options.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)));

            services.AddDbContext<BitcornGameContext>(options =>
                options.UseSqlServer(connection,
                Options => Options.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null)));

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            loggerFactory.AddSerilog();

            app.UseRouting();

            app.UseWebSockets();

            app.UseAuthentication();
            app.UseAuthorization();
            
            
            app.UseEndpoints(endpoints =>
            {
            
                endpoints.MapControllers().WithMetadata(new AllowAnonymousAttribute()); ;
            });
        }

    }
}
