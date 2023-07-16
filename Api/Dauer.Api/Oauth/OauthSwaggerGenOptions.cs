using Dauer.Api.Config;
using IdentityModel.Client;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dauer.Api.Oauth;

public class OauthSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly OauthConfig config_;

  public OauthSwaggerGenOptions
  (
    IHttpClientFactory httpClientFactory,
    OauthConfig config
  )
  {
    _httpClientFactory = httpClientFactory;
    config_ = config;
  }

  public void Configure(SwaggerGenOptions options)
  {
    var discoveryDocument = GetDiscoveryDocument();

    if (discoveryDocument == null) { return; }

    options.AddSecurityDefinition(config_.SecurityDefinitionName, new OpenApiSecurityScheme
    {
      Type = SecuritySchemeType.OAuth2,

      Flows = new OpenApiOAuthFlows
      {
        AuthorizationCode = new OpenApiOAuthFlow
        {
          AuthorizationUrl = new Uri(discoveryDocument.AuthorizeEndpoint ?? ""),
          TokenUrl = new Uri(discoveryDocument.TokenEndpoint ?? ""),
          Scopes = new Dictionary<string, string>
          {
              {"openid", "Open Id" },
          }
        }
      },
      Description = "Dauer.Api"
    });
    options.OperationFilter<AuthOperationFilter>(config_);
  }

  private DiscoveryDocumentResponse GetDiscoveryDocument()
  {
    var req = new DiscoveryDocumentRequest
    {
      Address = config_.Authority
    };
    req.Policy.ValidateEndpoints = false;

    return _httpClientFactory
      .CreateClient()
      .GetDiscoveryDocumentAsync(req)
      .GetAwaiter()
      .GetResult();
  }
}
