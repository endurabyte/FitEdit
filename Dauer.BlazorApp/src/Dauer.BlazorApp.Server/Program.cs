using System;
using Dauer.BlazorApp.Server.Data;
using Dauer.BlazorApp.Server.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using Npgsql.Logging;
using Lamar.Microsoft.DependencyInjection;

namespace Dauer.BlazorApp.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            
            NpgsqlLogManager.Provider = new SerilogNpgsqlLoggingProvider();

            try
            {
                var domain = configuration["Domain"];
                var builder = CreateHostBuilder(args);
                builder.UseUrls(
                    $"http://{domain}",
                    $"https://{domain}"
                );
                
                var host = builder.Build();
                    
                SeedDb(host);
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }

        private static void SeedDb(IWebHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var databaseInitializer = scope.ServiceProvider.GetService<IDatabaseInitializer>();
                databaseInitializer.SeedAsync().Wait();
            }
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(l => l.AddConsole(x => x.IncludeScopes = true))
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build())
                .UseLamar()
                .UseStartup<Startup>()
                .UseStaticWebAssets()
                .UseSerilog()
                .UseKestrel(options =>
                {
                    options.ConfigureHttpsDefaults(options =>
                        options.ServerCertificateSelector = (c, s) =>
                            LetsEncryptRenewalService.Certificate);
                });
    }
}
