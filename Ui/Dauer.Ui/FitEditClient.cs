using IdentityModel.Client;

namespace Dauer.Ui;

public interface IFitEditClient
{
  Task<bool> IsAuthenticatedAsync(string? accessToken, CancellationToken ct);
}

public class FitEditClient : IFitEditClient
{
  private readonly string api_;

  public FitEditClient(string api)
  {
    api_ = api;
  }

  /// <summary>
  /// Check can we reach our API e.g. hosted on fly.io; it in turn reaches out to Supabase to verify the JWT 
  /// </summary>
  public async Task<bool> IsAuthenticatedAsync(string? accessToken, CancellationToken ct)
  {
    if (accessToken == null) { return false; }
    using var client = new HttpClient { BaseAddress = new Uri(api_) };
    client.SetBearerToken(accessToken);
    var response = await client.GetAsync("auth", cancellationToken: ct);
    return response.IsSuccessStatusCode;
  }
}
