using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Dauer.Api;

public class AwsCognitoClient : IOauthClient
{
  private readonly HttpClient client_ = new(new LoggingHttpHandler(new HttpClientHandler()));

  private string authority_;
  private string clientId_;

  public AwsCognitoClient(string region, string userPoolId, string clientId)
  {
    authority_ = $"https://cognito-idp.{region}.amazonaws.com/{region}_{userPoolId}";
    clientId_ = clientId;
  }

  public void ConfigureJwt(JwtBearerOptions opts)
  {
    opts.Authority = authority_;

    // AWS Cognito does not set "aud" on the JWT. Instead it sets "client_id" which we validate manually
    //opts.Audience = clientId;

    opts.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateAudience = false,

      // Log Cognito requests and responses
      ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
        $"{opts.Authority.TrimEnd('/')}/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever(client_)
      ),
    };

    opts.Events = new JwtBearerEvents
    {
      // Validate "client_id" manually
      OnTokenValidated = ctx =>
      {
        var jwt = ctx.SecurityToken as JwtSecurityToken;
        return Validate(ctx, jwt);
      },
    };
  }

  private Task Validate(TokenValidatedContext ctx, JwtSecurityToken? jwt)
  {
    if (jwt == null) { ctx.Fail("No jwt found"); return Task.CompletedTask; }

    var claim = jwt.Claims.FirstOrDefault(c => c.Type == "client_id");

    if (claim == null) { ctx.Fail("No client_id given"); return Task.CompletedTask; }
    if (claim.Value != clientId_) { ctx.Fail("Invalid client_id"); }

    return Task.CompletedTask;
  }
}