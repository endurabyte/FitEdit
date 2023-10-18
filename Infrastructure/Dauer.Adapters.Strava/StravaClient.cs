using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Model.Strava;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Adapters.Strava;

public partial class StravaClient : ReactiveObject, IStravaClient
{
  [GeneratedRegex("meta name=\\\"csrf-token\\\" content=\\\"([+/=\\w]+)\\\"")]
  private static partial Regex GetCsrfTokenRegex();

  [GeneratedRegex("meta name=\\\"csrf-param\\\" content=\\\"([_\\w]+)\\\"")]
  private static partial Regex GetCsrfParamRegex();

  private const string BASE_URL = "https://www.strava.com";
  private readonly ILogger<StravaClient> log_;

  public StravaConfig Config { get; set; } = new();
  public Dictionary<string, Model.Cookie>? Cookies { get; set; } = new();

  [Reactive] public double AuthenticateProgress { get; private set; }
  [Reactive] public bool IsSignedIn { get; set; }

  public StravaClient(ILogger<StravaClient> log)
  {
    log_ = log;
  }

  public async Task<bool> AuthenticateAsync()
  {
    const double nsteps = 3.0;
    AuthenticateProgress = 0 / nsteps * 100;
    IsSignedIn = false;

    // Use the about page because it's small and doesn't redirect based on if the client is logged in or not.
    string url = $"{BASE_URL}/about";

    (string? csrfToken, string? csrfParam) = await GetCsrfTokenAsync(url);
    AuthenticateProgress = 1 / nsteps * 100;

    CookieContainer cookies = GetCachedCookies();
    HttpClient client = GetUnauthenticatedClient(cookies, allowAutoRedirect: false);
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

    var data = new Dictionary<string, string?>
    {
      { "email", Config.Username },
      { "password", Config.Password },
      { "remember_me", "on" },
      { csrfParam ?? "authenticity_token", csrfToken }
    };

    HttpResponseMessage resp = await client.PostAsJsonAsync($"{BASE_URL}/session", data);
    AuthenticateProgress = 2 / nsteps * 100;

    if ($"{resp.Headers.Location}" == $"{BASE_URL}/login")
    {
      log_.LogError("Could not log in to Strava. Got redirected to login page");
      return false;
    }

    Cookies = cookies.MapModel();
    if (!Cookies.TryGetValue("strava_remember_id", out Model.Cookie? tokenId))
    {
      log_.LogError("Could not log in to Strava. Could not find strava_remember_id");
      return false;
    }

    if (!Cookies.TryGetValue("strava_remember_token", out Model.Cookie? token))
    {
      log_.LogError("Could not log in to Strava. Could not find strava_remember_id");
      return false;
    }

    //string? jwt = token?.Value;

    bool isAuthenticated = await IsAuthenticatedAsync();
    AuthenticateProgress = 3 / nsteps * 100;
    return isAuthenticated;
  }

  private void LoginWithJwt(string jwt)
  {
    // The JWT's 'sub' key contains the id of the account.
    // This must be extracted and set as the 'strava_remember_id' cookie.
  }

  public async Task<bool> IsAuthenticatedAsync()
  {
    // Load user settings page. If we get redirected to login page, we are not authenticated.
    HttpClient client = GetAuthenticatedClient();
    HttpResponseMessage resp = await client.GetAsync($"{BASE_URL}/settings/profile");

    IsSignedIn = resp.RequestMessage?.RequestUri?.ToString() != $"{BASE_URL}/login";
    return IsSignedIn;
  }

  public Task<bool> LogoutAsync()
  {
    Cookies = new();
    IsSignedIn = false;
    return Task.FromResult(true);
  }

  public async Task<List<StravaActivity>> ListAllActivitiesAsync(CancellationToken ct = default)
  {
    HttpClient client = GetAuthenticatedClient();

    client.DefaultRequestHeaders.Add("Accept", "text/javascript");
    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

    const int perPage = 20; // capped to 20 by server
    int? total = null;
    var activities = new ConcurrentDictionary<long, StravaActivity>();

    // Get first page so we have a count
    async Task fetchPageAsync(int page, CancellationToken ct = default)
    {
      string url = $"{BASE_URL}/athlete/training_activities"
         + $"?keywords="
         + $"&activity_type="
         + $"&workout_type="
         + $"&commute="
         + $"&private_activities="
         + $"&trainer="
         + $"&gear="
         + $"&search_session_id={Guid.NewGuid()}"
         + $"&new_activity_only=false"
         + $"&page={page}" // Optional on first page
         + $"&per_page={perPage}"; // Optional on first page

      HttpResponseMessage resp = await client.GetAsync(url, ct);
      string json = await resp.Content.ReadAsStringAsync(ct);

      StravaTrainingActivitiesResponse? stravaResponse = Json.MapFromJson<StravaTrainingActivitiesResponse>(json);
      activities.AddRange(stravaResponse?.Models.Select(m => (m.Id, m)));

      total ??= stravaResponse?.Total ?? -1;
    };

    await fetchPageAsync(1, ct);
    total ??= 0;
    IEnumerable<int> range = Enumerable.Range(2, total.Value / perPage + 1);

    // Get remaining pages in parallel
    await Parallel.ForEachAsync(range, async (page, ct) =>
    {
      await fetchPageAsync(page, ct);
      log_.LogInformation($"Got {activities.Count} of {total} Strava activities ({(double)activities.Count/total * 100:#.#}%)");
    });

    return activities.Values.OrderByDescending(a => a.Id).ToList();
  }

  private async Task<(string?, string?)> GetCsrfTokenAsync(string url)
  {
    CookieContainer cookies = GetCachedCookies();
    HttpClient client = GetUnauthenticatedClient(cookies, allowAutoRedirect: false);

    HttpResponseMessage resp = await client.GetAsync(url);

    string html = await resp.Content.ReadAsStringAsync();
    string? csrfToken = GetCsrfTokenRegex().GetSingleValue(html, 2, 1);
    string? csrfParam = GetCsrfParamRegex().GetSingleValue(html, 2, 1);

    if (csrfToken is null)
    {
      log_.LogError("Could not find Garmin Connect CSRF token in HTML response {@data}", html);
    }

    return (csrfToken, csrfParam);
  }

  private HttpClient GetAuthenticatedClient(CookieContainer? cookies = null) => GetUnauthenticatedClient(cookies ?? GetCachedCookies());

  private CookieContainer GetCachedCookies() => Cookies?.MapCookieContainer() ?? new CookieContainer();

  private static HttpClient GetUnauthenticatedClient(CookieContainer cookies, bool allowAutoRedirect = true)
  {
    // Use SocketsHttpHandler to get consistent behavior across platforms.
    // For example, AndroidMessageHandler seems to only support HTTP/1.1 which Garmin rejects.
    var clientHandler_ = new SocketsHttpHandler
    {
      AllowAutoRedirect = allowAutoRedirect,
      UseCookies = true,
      CookieContainer = cookies,
    };

    var client = new HttpClient(clientHandler_)
    {
      DefaultRequestVersion = HttpVersion.Version20,
      DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
    };

    return client;
  }
}