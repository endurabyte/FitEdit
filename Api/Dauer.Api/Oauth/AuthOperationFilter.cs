using Dauer.Api.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dauer.Api.Oauth;

public class AuthOperationFilter : IOperationFilter
{
  private readonly OauthConfig config_;

  public AuthOperationFilter(OauthConfig config)
  {
    config_ = config;
  }

  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var authAttributes = context.MethodInfo
      .GetCustomAttributes(true)
      .OfType<AuthorizeAttribute>()
      .Distinct();

    if (!authAttributes.Any())
    {
      return;
    }

    operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
    operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

    var jwtbearerScheme = new OpenApiSecurityScheme
    {
      Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = config_.SecurityDefinitionName }
    };

    operation.Security = new List<OpenApiSecurityRequirement>
      {
        new OpenApiSecurityRequirement
        {
          [jwtbearerScheme] = Array.Empty<string>()
        }
      };
  }
}