using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluffySpoon.AspNet.LetsEncrypt.Certes;

namespace BlazorApp.Server
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

            try
            {
                //IdentityServer4 seed should be happening here but because of this bug https://github.com/aspnet/AspNetCore/issues/12349
                //the seeding is not implemented here.

                BuildWebHost(args, configuration["Domain"]).Run();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
        }

        public static IWebHost BuildWebHost(string[] args, string domain) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(l => l.AddConsole(x => x.IncludeScopes = true))
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build())
                .UseStartup<Startup>()
                .UseSerilog()
                .UseKestrel(options =>
                {
                    options.ConfigureHttpsDefaults(options => 
                        options.ServerCertificateSelector = (c, s) => 
                            LetsEncryptRenewalService.Certificate);
                })
                .UseUrls(
                    $"http://{domain}", 
                    $"https://{domain}"
                )
                .Build();
    }
}
