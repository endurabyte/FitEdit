using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Dauer.Api.Controllers;

public class OutsetaAuthorization
{
  public string access_token { get; set; }
  public int expires_in { get; set; }
  public string token_type { get; set; }
}

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
  private readonly ILogger<AuthController> _logger;

  public AuthController(ILogger<AuthController> logger)
  {
    _logger = logger;
  }

  [HttpGet(Name = "GetAuthorization")]
  public async Task<Authorization> Get()
  {
    var client = new HttpClient();
    var request = new HttpRequestMessage(HttpMethod.Post, "https://fitedit.outseta.com/tokens");
    request.Headers.Add("Authorization", "Outseta {{ee625988-c683-4a2b-9942-a1e366ad63ef}}:{{5a95c4892c4e03218672cbec0fc94bb2}}");
    var collection = new List<KeyValuePair<string, string>>
    {
      new("username", "dougslater@gmail.com")
    };
    var content = new FormUrlEncodedContent(collection);
    request.Content = content;

    try
    {
      var response = await client.SendAsync(request);
      Info($"Got response {response.StatusCode}");
      string responseContent = await response.Content.ReadAsStringAsync();
      Info(responseContent);

      var json = JsonSerializer.Deserialize<OutsetaAuthorization>(responseContent);
      return new Authorization
      {
        AccessToken = json?.access_token,
      };
    }
    catch (Exception e)
    {
      Info($"{e}");
      throw;
    }
  }

  private void Info(string message) => _logger.LogInformation(message);
}