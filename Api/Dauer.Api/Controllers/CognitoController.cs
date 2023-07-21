using System.Text;
using Amazon.Lambda.CognitoEvents;
using Dauer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dauer.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CognitoController : ControllerBase
{
  private readonly ILogger<CognitoController> log_;
  private readonly IUserService users_;

  /// <summary>
  /// The key AWS Lambda provides to us for Cognito events
  /// </summary>
  public static string? ApiKey { get; set; }

  public CognitoController(ILogger<CognitoController> log, IUserService users)
  {
    log_ = log;
    users_ = users;
  }

  [HttpPost("event")]
  public async Task Event([FromBody] CognitoPreSignupEvent e)
  {
    if (!Request.Headers.TryGetValue("X-API-KEY", out var apiKey) && apiKey.Any(key => key == ApiKey))
    {
      Response.StatusCode = 401;
      return;
    }

    bool haveName = e.Request.UserAttributes.TryGetValue("name", out string? name);
    bool haveEmail = e.Request.UserAttributes.TryGetValue("email", out string? email);

    if (!haveEmail)
    {
      Response.StatusCode = 400;
      Response.Body = new MemoryStream(Encoding.UTF8.GetBytes("Missing email"));
      return;
    }

    Model.User? user = await users_.FindAsync(email).ConfigureAwait(false);

    if (user != null && user.Email == email)
    {
      Response.StatusCode = 400;
      Response.Body = new MemoryStream(Encoding.UTF8.GetBytes("Email already exists"));
      return;
    }

    await users_.AddOrUpdate(new Model.User
    {
      Name = name,
      Email = email,
      CognitoId = e.UserName,
    });
  }
}