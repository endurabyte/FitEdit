using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dauer.Api;

public class AuthOperationFilter : IOperationFilter
{
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
      Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" }
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