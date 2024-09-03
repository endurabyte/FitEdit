using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FitEdit.Model;
using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using FitEdit.Model.Strava;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Adapters.Strava;

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
    HttpClient client = GetUnauthenticatedClient(cookies, allowAutoRedirect: true);
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");

    var data = new Dictionary<string, string?>
    {
      { "email", Config.Username },
      { "password", Config.Password },
      { "remember_me", "on" },
      { csrfParam ?? "authenticity_token", csrfToken }
    };

    using var content = new FormUrlEncodedContent(data
      .Where(kv => kv.Value != null)
      .ToDictionary(kv => kv.Key, kv => kv.Value!));

    HttpResponseMessage resp = await client.PostAsync($"{BASE_URL}/session", content); // Should be HTTP 302, redirect to /dashboard
    AuthenticateProgress = 2 / nsteps * 100;

    // Should be /dashboard if we successfully signed in, else redirected back to /login
    if ($"{resp.Headers.Location}" == $"{BASE_URL}/login")
    {
      log_.LogError("Could not log in to Strava. Got redirected to login page");
      return false;
    }

    Cookies = cookies.MapModel();
    if (!Cookies.TryGetValue("_strava4_session", out Model.Cookie? session))
    {
      log_.LogError("Could not log in to Strava. Could not find the expected cookie '_strava4_session'.");
      return false;
    }

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

  public async Task<List<StravaActivity>> ListAllActivitiesAsync(UserTask task, CancellationToken ct = default)
  {
    HttpClient client = GetAuthenticatedClient();

    client.DefaultRequestHeaders.Add("Accept", "text/javascript");
    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

    const int perPage = 20; // capped by server: sending a greater value still returns 20.
    int? total = null;
    var activities = new ConcurrentDictionary<long, StravaActivity>();

    bool tooMany = false;

    // Get first page so we have a total
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

      if (resp.StatusCode == HttpStatusCode.TooManyRequests)
      {
        tooMany = true;
        task.Cancel(); // Auto-dismiss
        task.Status = "Strava says we've made too many requests. Try again later.";
        return;
      }

      StravaTrainingActivitiesResponse? stravaResponse = Json.MapFromJson<StravaTrainingActivitiesResponse>(json);
      if (stravaResponse is not null)
      {
        var toAdd = stravaResponse.Models.Select(m => (m.Id, m));
        activities.AddRange(toAdd);
      }

      total ??= stravaResponse?.Total ?? -1;
    };

    await fetchPageAsync(1, ct);
    total ??= 0;
    IEnumerable<int> range = Enumerable.Range(2, total.Value / perPage + 1);

    // Get remaining pages in parallel
    await Parallel.ForEachAsync(range, async (page, ct) =>
    {
      if (tooMany) { return; }
      await fetchPageAsync(page, ct);
      task.Status = $"Got {activities.Count} of {total} Strava activities ({(double)activities.Count/total * 100:#.#}%)";
    });

    return activities.Values.OrderByDescending(a => a.Id).ToList();
  }

  public async Task<byte[]> DownloadActivityFileAsync(long id, CancellationToken ct = default)
  {
    string url = $"https://www.strava.com/activities/{id}/export_original";

    HttpClient client = GetAuthenticatedClient();
    HttpResponseMessage resp = await client.GetAsync(url, ct);

    byte[] bytes = await resp.Content.ReadAsByteArrayAsync(ct);
    return bytes;
  }

  public async Task<(bool Success, long ActivityId)> UploadActivityAsync(Stream stream)
  {
    string url = $"https://www.strava.com/upload/files";
    string aboutUrl = $"{BASE_URL}/about";
    (string? csrfToken, _) = await GetCsrfTokenAsync(aboutUrl);

    var form = new MultipartFormDataContent($"---------------------------");

    using var methodContent = new StringContent("post");
    using var authenticityTokenContent = new StringContent(csrfToken ?? "");
    using var content = new StreamContent(stream);

    methodContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    {
      Name = "_method"
    };

    authenticityTokenContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    {
      Name = "authenticity_token"
    };

    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    {
      Name = "files[]",
      FileName = Path.GetFileName("fitedit-upload.fit"),
      Size = stream.Length,
    };
    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

    form.Add(methodContent);
    form.Add(authenticityTokenContent);
    form.Add(content);

    HttpClient client = GetAuthenticatedClient();
    HttpResponseMessage resp = await client.PostAsync(url, form);
    StravaUploadResponse? response = await resp.Content.MapFromJson<StravaUploadResponse>();

    // Poll for upload status
    while (true) 
    {
      StravaUploadStatus? status = response?.FirstOrDefault();

      if (status == null) { return (false, -1); }
      log_.LogInformation("Strava upload progress: {id} {workflow} {progress}", status.Id, status.Workflow, status.Progress);
      if (status?.Workflow == "error") 
      {
        log_.LogError("Error uploading to Strava. {error}", status?.Error);
        return (false, -1); 
      }
      if (status?.Workflow == "uploaded") { return (true, status?.Activity?.Id ?? -1); }

      await Task.Delay(2000);
      response = await GetUploadStatus(status?.Id ?? 0);
    }
  }

  public async Task<bool> DeleteActivityAsync(long id)
  {
    string url = $"https://www.strava.com/athlete/training_activities/{id}";
    string aboutUrl = $"{BASE_URL}/about";
    (string? csrfToken, _) = await GetCsrfTokenAsync(aboutUrl);

    HttpClient client = GetAuthenticatedClient();
    client.DefaultRequestHeaders.Add("Accept", "text/javascript");
    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
    client.DefaultRequestHeaders.Add("X-CSRF-Token", csrfToken);

    HttpResponseMessage resp = await client.DeleteAsync(url);
    return resp.StatusCode == HttpStatusCode.OK;
  }

  private async Task<StravaUploadResponse?> GetUploadStatus(long id)
  {
    string url = $"https://www.strava.com/upload/progress.json?ids[]={id}";
    HttpClient client = GetAuthenticatedClient();
    HttpResponseMessage resp = await client.GetAsync(url);
    return await resp.Content.MapFromJson<StravaUploadResponse>();
  }

  private async Task<(string?, string?)> GetCsrfTokenAsync(string url)
  {
    CookieContainer cookies = GetCachedCookies();
    HttpClient client = GetUnauthenticatedClient(cookies, allowAutoRedirect: false);
    HttpResponseMessage resp = await client.GetAsync(url);
    Cookies = cookies.MapModel();

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