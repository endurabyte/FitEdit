using System.Diagnostics;
using System.Runtime.InteropServices;
using Dauer.Api.Config;
using Dauer.Api.Controllers;
using Dauer.Api.Data;
using Dauer.Api.Oauth;
using Dauer.Api.Services;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SendGrid;
using Serilog;
using Stripe;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dauer.Api;

public class CompositionRoot
{

}

public static class Program
{
  public static async Task Main(string[] args)
  {
    // Determine environment
    string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    bool isProduction = !(Debugger.IsAttached || env == Environments.Development);

    string os = RuntimeInformation.OSDescription;
    os = os switch
    {
      _ when os.Contains("Windows", StringComparison.OrdinalIgnoreCase) => "Windows",
      _ when os.Contains("mac", StringComparison.OrdinalIgnoreCase) => "macOS",
      _ => "Linux",
    };

    // Load config
    IConfiguration configuration = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json")
       .AddJsonFile($"appsettings.{env}.json", true)
       .AddJsonFile($"appsettings.{os}.json", true)
       .AddEnvironmentVariables()
       .Build();

    // Bootstrap log
    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .CreateBootstrapLogger();
    ILoggerFactory factory = new LoggerFactory().AddSerilog(logger);
    var log = factory.CreateLogger("Bootstrap");

    CognitoController.ApiKey = Environment.GetEnvironmentVariable("DAUER_API_KEY") ?? "";
    StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? "";
    string stripeEndpointSecret = Environment.GetEnvironmentVariable("STRIPE_ENDPOINT_SECRET") ?? "";
    string sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? "";

    string awsRegion = configuration["Dauer:OAuth:AwsRegion"] ?? "";
    string userPoolId = configuration["Dauer:OAuth:UserPoolId"] ?? "";
    string clientId = configuration["Dauer:OAuth:ClientId"] ?? "";
    string securityDefinitionName = configuration["Dauer:OAuth:SecurityDefinitionName"] ?? "";

    string connectionString = configuration["ConnectionStrings:Default"] ?? "";
    bool useSqlite = configuration.GetValue<bool>("UseSqlite");

    bool isFly = configuration["FLY_APP_NAME"] != null;
    string? dbUrl = configuration["DATABASE_URL"];

    if (isFly && dbUrl != null)
    {
      log.LogInformation($"Detected we are hosted on fly.io");
      connectionString = ConvertPgUrlToDbConnString(dbUrl);
    }

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();

    // Db
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
      string conn = connectionString;

      if (!useSqlite && isProduction) { options.UseNpgsql(conn); }
      else 
      {
        // Replace the "%localappdata%" placeholder with the actual path
        conn = conn.Replace("%localappdata%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        // Parse the connection string to get the directory
        var directory = Path.GetDirectoryName(conn.Substring("Data Source=".Length));

        // Check if the directory exists, if not create it
        if (directory != null && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }
        options.UseSqlite(conn); 
      }
    });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    // IHttpClientFactory
    builder.Services.AddHttpClient();

    // Oauth
    var oauthConfig = new OauthConfig
    {
      AwsRegion = awsRegion,
      UserPoolId = userPoolId,
      ClientId = clientId,
      SecurityDefinitionName = securityDefinitionName,
    };

    var cognito = new AwsCognitoClient(oauthConfig);

    builder.Services
      .AddAuthentication()
      .AddJwtBearer(cognito.ConfigureJwt);

    // Debug
    if (!isProduction)
    {
      // Configure Let's Encrypt client
      // Not needed when hosted on fly.io so we only use it in debug
      builder.Services.AddLettuceEncrypt();
      builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    }

    // IoC 
    builder.Host.UseLamar((context, registry) =>
    {
      registry.For<IUserRepo>().Use<UserRepo>();
      registry.For<IOauthClient>().Use(cognito);
      registry.For<IConfigureOptions<SwaggerGenOptions>>().Use<OauthSwaggerGenOptions>();
      registry.For<OauthConfig>().Use(oauthConfig);
      registry.For<StripeConfig>().Use(new StripeConfig { EndpointSecret = stripeEndpointSecret });
      registry.For<IEmailService>().Use<SendGridEmailService>();
      registry.For<SendGridClient>().Use<SendGridClient>().Ctor<string>().Is(sendGridApiKey);
    });

    var app = builder.Build();

    var db = app.Services.GetService<AppDbContext>();
    log = app.Services.GetService<ILogger<CompositionRoot>>();
    log.LogInformation($"Database connection string: {connectionString}");

    if (db != null)
    {
      await db.InitAsync().ConfigureAwait(false);
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseStaticFiles(); // to serve SwaggerDark.css
      app.UseSwagger();

      app.UseSwaggerUI(c =>
      {
        c.InjectStylesheet("/swagger-ui/SwaggerDark.css");
        c.OAuthClientId(clientId);
        c.OAuthScopes("openid");
        c.OAuthUsePkce();
      });

      app.UseDeveloperExceptionPage();
      app.UseMigrationsEndPoint();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
  }

  private static string ConvertPgUrlToDbConnString(string? dbUrl)
  {
    if (dbUrl == null) { return ""; }
    
    // Example
    //dbUrl = "postgres://fitedit:supersecret@fitedit-pg.flycast:5432/fitedit?sslmode=disable";

    var uri = new Uri(dbUrl);
    string[] userInfo = uri.UserInfo.Split(':');
    string sslMode = string.IsNullOrEmpty(uri.Query) ? "": uri.Query.Replace("?sslmode=", "");

    return $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
      $"User Id={userInfo[0]};Password={userInfo[1]};SSL Mode={sslMode}";
  }
}