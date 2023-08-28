using System.Text.Json;
using System.Web;
using Dauer.Model;
using Dauer.Model.Clients;
using Dauer.Model.Web;
using IdentityModel.Client;

namespace Dauer.Ui.Infra;

public class FitEditClient : IFitEditClient
{
  private readonly string api_;
  private readonly IBrowser browser_;

  public string AccessToken { get; set; } = "";

  public FitEditClient(string api, IBrowser browser)
  {
    api_ = api;
    browser_ = browser;
  }

  /// <summary>
  /// Check if can we reach our JWT-secured API e.g. hosted on fly.io.
  /// </summary>
  public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
  {
    using var client = new HttpClient { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken);
    var response = await client.GetAsync("auth", cancellationToken: ct);
    return response.IsSuccessStatusCode;
  }

  public async Task<bool> AuthorizeGarminAsync(string? username, CancellationToken ct = default)
  {
    var client = new HttpClient() { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken);
    var responseMsg = await client.GetAsync($"garmin/oauth/init?username={HttpUtility.UrlEncode(username)}", ct);

    if (!responseMsg.IsSuccessStatusCode)
    {
      return false;
    }

    try
    {
      var content = await responseMsg.Content.ReadAsStringAsync(ct);
      var token = JsonSerializer.Deserialize<OauthToken>(content);

      if (token?.Token == null) { return false; }

      // Open browser to Garmin auth page
      string url = $"https://connect.garmin.com/oauthConfirm" +
        $"?oauth_token={token?.Token}" +
        $"&oauth_callback={HttpUtility.UrlEncode($"{api_}garmin/oauth/complete?username={username}")}" +
        $"";

      await browser_.OpenAsync(url);
      return true;
    }
    catch (JsonException e)
    {
      Log.Error($"Error authorizing Garmin: {e}");
    }
    catch (Exception e)
    {
      Log.Error($"Error authorizing Garmin: {e}");
    }

    return false;
  }

  public async Task<bool> DeauthorizeGarminAsync(string? username, CancellationToken ct = default)
  {
    var client = new HttpClient { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken);
    var responseMsg = await client.PostAsync($"garmin/oauth/deregister?username={HttpUtility.UrlEncode(username)}", null, cancellationToken: ct);

    if (!responseMsg.IsSuccessStatusCode)
    {
      string? err = await responseMsg.Content.ReadAsStringAsync(ct);
      Log.Error(err);
      return false;
    }

    return true;
  }

  public async Task<bool> AuthorizeStravaAsync(string? username, CancellationToken ct = default)
  {
    var client = new HttpClient { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken);

    try
    {

      var responseMsg = await client.GetAsync($"strava/oauth/init?username={HttpUtility.UrlEncode(username)}", ct);

      if (!responseMsg.IsSuccessStatusCode)
      {
        return false;
      }

      // Open browser to Strava auth page
      string url = await responseMsg.Content.ReadAsStringAsync(ct);
      await browser_.OpenAsync(url);
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<bool> DeauthorizeStravaAsync(string? username, CancellationToken ct = default)
  {
    var client = new HttpClient { BaseAddress = new Uri(api_) };
    client.SetBearerToken(AccessToken);

    try
    {

      var responseMsg = await client.DeleteAsync($"strava/oauth/token?username={HttpUtility.UrlEncode(username)}", ct);

      if (!responseMsg.IsSuccessStatusCode)
      {
        return false;
      }

      return true;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

}
