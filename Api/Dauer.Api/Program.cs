using Lamar.Microsoft.DependencyInjection;

namespace Dauer.Api;

public static class Program
{
  public static void Main(string[] args)
  {
    var cognito = new AwsCognitoClient("us-east-1", "nqQT8APwr", "5n3lvp2jfo1c2kss375jvkhvod");
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure Let's Encrypt client
    builder.Services.AddLettuceEncrypt();

    // Configure Oauth
    builder.Services
      .AddAuthentication()
      .AddJwtBearer(cognito.ConfigureJwt);

    builder.Host.UseLamar((context, registry) =>
    {
      registry.For<IOauthClient>().Use(cognito);
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
  }
}