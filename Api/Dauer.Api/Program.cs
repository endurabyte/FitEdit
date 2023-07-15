using System.Diagnostics;
using Dauer.Api.Config;
using Dauer.Api.Data;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Stripe;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dauer.Api;

public static class Program
{
  public static void Main(string[] args)
  {
    string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    IConfiguration configuration = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json")
       .AddJsonFile($"appsettings.{env}.json", true)
       .Build();

    string awsRegion = configuration["Dauer:OAuth:AwsRegion"] ?? "";
    string userPoolId = configuration["Dauer:OAuth:UserPoolId"] ?? "";
    string clientId = configuration["Dauer:OAuth:ClientId"] ?? "";
    string securityDefinitionName = configuration["Dauer:OAuth:SecurityDefinitionName"] ?? "";
    string stripeEndpointSecret = configuration["Dauer:Stripe:EndpointSecret"] ?? "";
    string connectionString = configuration["ConnectionStrings:Default"] ?? "";

    var oauthConfig = new OauthConfig
    {
      AwsRegion = awsRegion,
      UserPoolId = userPoolId,
      ClientId = clientId,
      SecurityDefinitionName = securityDefinitionName,
    };

    string apiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? "";
    StripeConfiguration.ApiKey = apiKey;

    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .CreateBootstrapLogger();

    ILoggerFactory factory = new LoggerFactory().AddSerilog(logger);

    var cognito = new AwsCognitoClient(oauthConfig);

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();

    builder.Services.AddDbContext<DataContext>(options =>
    {
      options.UseNpgsql(connectionString);
    });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    if (Debugger.IsAttached || env == Environments.Development)
    {
      // Configure Let's Encrypt client
      // Not needed when hosted on fly.io so we only use it in debug
      builder.Services.AddLettuceEncrypt(); 
    }

    // Configure Oauth
    builder.Services
      .AddAuthentication()
      .AddJwtBearer(cognito.ConfigureJwt);

    // Inject IHttpClientFactory
    builder.Services.AddHttpClient();

    builder.Host.UseLamar((context, registry) =>
    {
      registry.For<IOauthClient>().Use(cognito);
      registry.For<IConfigureOptions<SwaggerGenOptions>>().Use<DauerSwaggerGenOptions>();
      registry.For<OauthConfig>().Use(oauthConfig);
      registry.For<StripeConfig>().Use(new StripeConfig { EndpointSecret = stripeEndpointSecret });
    });

    var app = builder.Build();

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
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
  }
}