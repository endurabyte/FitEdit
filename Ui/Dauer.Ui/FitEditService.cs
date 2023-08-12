using System.Text.Json;
using System.Web;
using Dauer.Model;
using Dauer.Ui.Supabase;
using IdentityModel.Client;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui;

public interface IFitEditService
{
  bool IsAuthenticatedWithGarmin { get; }
  Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
  Task AuthorizeGarminAsync(string? username, CancellationToken ct = default);
  Task<bool> DeauthorizeGarminAsync(string? username ,CancellationToken ct = default);
}

public class NullFitEditService : IFitEditService
{
  public bool IsAuthenticatedWithGarmin => false;
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(true);
  public Task AuthorizeGarminAsync(string? username, CancellationToken ct = default) => Task.CompletedTask;
  public Task<bool> DeauthorizeGarminAsync(string? username, CancellationToken ct = default) => Task.FromResult(true);
}

public class FitEditService : ReactiveObject, IFitEditService
{
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }

  private readonly ISupabaseAdapter supa_;
  private readonly string api_;

  private string AccessToken_ => supa_.Authorization?.AccessToken ?? "";

  public FitEditService(ISupabaseAdapter supa, string api)
  {
    supa_ = supa;
    api_ = api;
    
    supa_
      .ObservableForProperty(x => x.IsAuthenticatedWithGarmin)
      .Subscribe(_ => IsAuthenticatedWithGarmin = supa_.IsAuthenticatedWithGarmin);
  }

  /// <summary>
  /// Check can we reach our JWT-secured API e.g. hosted on fly.io.
  /// </summary>
  public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
  {
    using var client = new HttpClient { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken_);
    var response = await client.GetAsync("auth", cancellationToken: ct);
    return response.IsSuccessStatusCode;
  }

  public async Task AuthorizeGarminAsync(string? username, CancellationToken ct)
  {
    var client = new HttpClient() { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken_);
    var responseMsg = await client.GetAsync($"garmin/oauth/init?username={HttpUtility.UrlEncode(username)}", ct);

    if (!responseMsg.IsSuccessStatusCode)
    {
      return;
    }

    try
    {
      var content = await responseMsg.Content.ReadAsStringAsync(ct);
      var token = JsonSerializer.Deserialize<OauthToken>(content);

      if (token?.Token == null) { return; }

      // Open browser to Garmin auth page
      string url = $"https://connect.garmin.com/oauthConfirm" +
        $"?oauth_token={token?.Token}" +
        $"&oauth_callback={HttpUtility.UrlEncode($"{api_}garmin/oauth/complete?username={username}")}" +
        $"";

      Browser.Open(url);
    }
    catch (JsonException e)
    {
      Log.Error($"Error authorizing Garmin: {e}"); 
    }
    catch (Exception e)
    {
      Log.Error($"Error authorizing Garmin: {e}"); 
    }
  }

  public async Task<bool> DeauthorizeGarminAsync(string? username, CancellationToken ct = default)
  {
    var client = new HttpClient { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken_);
    var responseMsg = await client.PostAsync($"garmin/oauth/deregister?username={HttpUtility.UrlEncode(username)}", null, cancellationToken: ct);

    if (!responseMsg.IsSuccessStatusCode)
    {
      string? err = await responseMsg.Content.ReadAsStringAsync(ct);
      Log.Error(err);
      return false;
    }

    IsAuthenticatedWithGarmin = false;
    return true;
  }
}
