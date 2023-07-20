using Amazon.Lambda.CognitoEvents;
using Microsoft.AspNetCore.Mvc;

namespace Dauer.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CognitoController : ControllerBase
{
  /// <summary>
  /// The key AWS Lambda provides to us for Cognito events
  /// </summary>
  public static string? ApiKey { get; set; }

  [HttpPost("event")]
  public async Task Event([FromBody] CognitoPreSignupEvent e)
  {
    if (!Request.Headers.TryGetValue("X-API-KEY", out var apiKey) && apiKey.Any(key => key == ApiKey))
    {
      Response.StatusCode = 401;
      return;
    }
    await Task.CompletedTask;
  }
}