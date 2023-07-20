using Amazon.Lambda.CognitoEvents;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Dauer.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CognitoController : ControllerBase
{
  private readonly ILogger<CognitoController> log_;

  /// <summary>
  /// The key AWS Lambda provides to us for Cognito events
  /// </summary>
  public static string? ApiKey { get; set; }

  public CognitoController(ILogger<CognitoController> log)
  {
    log_ = log;
  }

  [HttpPost("event")]
  public async Task Event([FromBody] CognitoPreSignupEvent e)
  {
    if (!Request.Headers.TryGetValue("X-API-KEY", out var apiKey) && apiKey.Any(key => key == ApiKey))
    {
      Response.StatusCode = 401;
      return;
    }

    var opts = new CustomerCreateOptions
    {
      Name = e.Request.UserAttributes["name"],
      Email = e.Request.UserAttributes["email"],
    };

    var service = new CustomerService();
    Customer customer = await service.CreateAsync(opts).ConfigureAwait(false);

    log_.LogInformation($"Created customer {customer.Id} for {opts.Name} ({opts.Email})");

    await Task.CompletedTask;
  }
}