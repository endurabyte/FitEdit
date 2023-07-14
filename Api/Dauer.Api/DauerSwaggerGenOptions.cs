using IdentityModel.Client;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dauer.Api;

public class DauerSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
  private readonly IHttpClientFactory _httpClientFactory;

  public DauerSwaggerGenOptions
  (
    IHttpClientFactory httpClientFactory
  )
  {
    _httpClientFactory = httpClientFactory;
  }

  public void Configure(SwaggerGenOptions options)
  {
    var discoveryDocument = GetDiscoveryDocument();

    if (discoveryDocument == null) { return; }

    options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
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
      Description = "Balea Server OpenId Security Scheme"
    });
    options.OperationFilter<AuthOperationFilter>();
  }

  private DiscoveryDocumentResponse GetDiscoveryDocument()
  {
    string authority = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_nqQT8APwr";

    var req = new DiscoveryDocumentRequest
    {
      Address = authority
    };
    req.Policy.ValidateEndpoints = false;

    return _httpClientFactory
      .CreateClient()
      .GetDiscoveryDocumentAsync(req)
      .GetAwaiter()
      .GetResult();
  }
}
