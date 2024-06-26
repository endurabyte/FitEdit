﻿using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FitEdit.Model;
using FitEdit.Model.Abstractions;
using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using FitEdit.Model.GarminConnect;
using FitEdit.Model.Web;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Adapters.GarminConnect;

public partial class GarminConnectClient : ReactiveObject, IGarminConnectClient
{
  private const string LOCALE = "en-US";
  private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/116.0";
  private const string CONNECT_URL = "https://connect.garmin.com";
  private const string CONNECT_URL_MODERN = CONNECT_URL + "/modern";
  private const string URL_UPLOAD = CONNECT_URL + "/upload-service/upload";
  private const string URL_ACTIVITY_BASE = CONNECT_URL + "/activity-service/activity";

  private const string UrlFitnessStats = "https://connect.garmin.com/fitnessstats-service/activity";
  private const string UrlActivityTypes = "https://connect.garmin.com/activity-service/activity/activityTypes";
  private const string UrlEventTypes = "https://connect.garmin.com/activity-service/activity/eventTypes";
  private const string UrlActivitiesBase = "https://connect.garmin.com/activitylist-service/activities/search/activities";
  private const string UrlActivityDownloadFile = "https://connect.garmin.com/download-service/export/{0}/activity/{1}";
  private const string UrlActivityDownloadDefaultFile = "https://connect.garmin.com/download-service/files/activity/{0}";

  private const ActivityFileType DefaultFile = ActivityFileType.Fit;

  private static readonly Dictionary<string, string> QueryParams = new()
  {
    {"clientId", "GarminConnect"},
    {"locale", LOCALE},
    {"service", CONNECT_URL_MODERN},
  };

  /// <summary>
  /// The logger
  /// </summary>
  private readonly ILogger<GarminConnectClient> log_;
  private readonly ITcpService tcp_;
  private readonly IBrowser browser_;

  public GarminConnectConfig Config { get; set; } = new();

  [Reactive] public Dictionary<string, Model.Cookie> Cookies { get; set; }
  [Reactive] public bool IsSignedIn { get; set; }

  /// <summary>
  /// Initializes a new instance of the <see cref="GarminConnectClient"/> class.
  /// </summary>
  /// <param name="config">The configuration.</param>
  /// <param name="log">The logger.</param>
  public GarminConnectClient(ILogger<GarminConnectClient> log, ITcpService tcp, IBrowser browser)
  {
    log_ = log;
    tcp_ = tcp;
    browser_ = browser;

    Cookies = new Dictionary<string, Model.Cookie>();
  }

  private CookieContainer GetCachedCookies(string domain) => Cookies.MapCookieContainer(domain);

  private static HttpClient GetUnauthenticatedClient(CookieContainer cookies)
  {
    // Use SocketsHttpHandler to get consistent behavior across platforms.
    // For example, AndroidMessageHandler seems to only support HTTP/1.1 which Garmin rejects.
    var clientHandler_ = new SocketsHttpHandler
    {
      AllowAutoRedirect = true,
      UseCookies = true,
      CookieContainer = cookies,
    };

    var client = new HttpClient(clientHandler_)
    {
      DefaultRequestVersion = HttpVersion.Version20,
      DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
    };
    client.DefaultRequestHeaders.Add("user-agent", USER_AGENT);

    return client;
  }

  private async Task<HttpClient> GetAuthenticatedClient(CookieContainer cookies = null, bool withAuthToken = true, CancellationToken ct = default)
  {
    cookies ??= GetCachedCookies(null);

    // User wants to login manually, i.e. with SSO GUID and SESSIONID cookie values
    // instead of user/pass.
    if (Config.SsoId is not null && Config.SessionId is not null)
    {
      cookies.Add(new Model.Cookie
      {
        Name = "GARMIN-SSO",
        Domain = ".garmin.com",
        Path = "/",
        Value = "1",
      }.MapSystemCookie());

      cookies.Add(new Model.Cookie
      {
        Name = "GARMIN-SSO-CUST-GUID",
        Domain = ".garmin.com",
        Path = "/",
        Value = Config.SsoId,
      }.MapSystemCookie());

      cookies.Add(new Model.Cookie
      {
        Name = "SESSIONID",
        Domain = "connect.garmin.com",
        Path = "/",
        Value = Config.SessionId,
      }.MapSystemCookie());

      cookies.Add(new Model.Cookie
      {
        Name = "JWT_FGP",
        Domain = ".connect.garmin.com",
        Path = "/",
        Value = Config.JwtId, // This will be null the first time this method runs, and nonnull after the first access token exchange.
      }.MapSystemCookie());
    }

    HttpClient client = GetUnauthenticatedClient(cookies);
    client.DefaultRequestHeaders.Add("DNT", "1");

    // Sets some cloudflare cookies
    HttpResponseMessage init = await client.GetAsync("https://connect.garmin.com", ct);
    Cookies = cookies.MapModel();

    if (!withAuthToken)
    {
      return client;
    }

    client.DefaultRequestHeaders.Add("NK", "NT");

    // Refresh token if necessary.
    if (Config?.Token?.ExpiresAt < DateTime.UtcNow + TimeSpan.FromMinutes(5))
    {
      if (Config?.Token.RefreshTokenExpiresAt > DateTime.UtcNow)
      {
        var refreshResp = await client.PostAsync(
          "https://connect.garmin.com/services/auth/token/refresh",
          new StringContent(JsonSerializer.Serialize(new
          {
            refresh_token = Config?.Token?.RefreshToken
          }), new MediaTypeHeaderValue("application/json")), 
          ct
        );

        Config.Token = Json.MapFromJson<GarminAccessToken>(await refreshResp.Content.ReadAsStringAsync(ct));

        // Also updates JWT_FGP cookie
        Cookies = cookies.MapModel();
        if (Cookies.TryGetValue("JWT_FGP", out Model.Cookie cookie))
        {
          Config.JwtId = cookie.Value;
        }
      }
    }

    // Use existing auth token
    if (Config.Token?.AccessToken is not null)
    {
      client.DefaultRequestHeaders.Add("DI-Backend", "connectapi.garmin.com");
      client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.Token?.AccessToken}");
      return client;
    }

    // Exchange SESSIONID for access token

    if (!cookies.ValidateCookiePresence("SESSIONID", CONNECT_URL_MODERN))
    {
      return client;
    }

    // This header must not be present for the exchange, and it must be present after
    client.DefaultRequestHeaders.Remove("DI-Backend"); 
    
    // Service is flaky. Try twice.
    Config.Token = await ExchangeForToken(client) ?? await ExchangeForToken(client);

    if (Config.Token?.AccessToken is not null)
    {
      client.DefaultRequestHeaders.Add("DI-Backend", "connectapi.garmin.com");
      client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.Token?.AccessToken}");
    }

    // Save the JWT_FGP cookie
    Cookies = cookies.MapModel();
    {
      if (Cookies.TryGetValue("JWT_FGP", out Model.Cookie cookie))
      {
        Config.JwtId = cookie.Value;
      }
    }
    return client;
  }

  private bool TryGetServiceTicketId(string json, out string serviceTicketId)
  {
    serviceTicketId = null;

    var loginResponse = Json.MapFromJson<GarminLoginResponse>(json);
    var loginError = Json.MapFromJson<GarminLoginError>(json);

    log_.LogInformation("Login response: {@loginResponse}", loginResponse);

    if (loginError?.Error is not null)
    {
      log_.LogError("Login error: {@loginError}", loginError);
      return false;
    }

    if (loginResponse?.ServiceTicketId is null)
    {
      log_.LogError("No Garmin login ticket");
      return false;
    }

    serviceTicketId = loginResponse.ServiceTicketId;
    return true;
  }

  public Task<bool> LogoutAsync()
  {
    Config = new();
    Cookies = new Dictionary<string, Model.Cookie>();
    IsSignedIn = false;
    return Task.FromResult(true);
  }

  /// <summary>
  /// Exchange SESSIONID for oauth token
  /// </summary>
  private async Task<GarminAccessToken> ExchangeForToken(HttpClient client, CancellationToken ct = default)
  {
    string url = $"{CONNECT_URL}/modern/di-oauth/exchange";

    try
    {
      // Add some jitter; the endpoint seems sensitive to being called too quickly
      await Task.Delay(500);
      HttpResponseMessage res = await client.PostAsync(url, null, ct);

      if (!res.IsSuccessStatusCode)
      {
        return null;
      }

      var token = await res.Content.ReadFromJsonAsync<GarminAccessToken>(cancellationToken: ct);
      token.ExpiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(token.ExpiresIn);
      token.RefreshTokenExpiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(token.RefreshTokenExpiresIn);

      return token;
    }
    catch (Exception e)
    {
      log_.LogError($"{nameof(ExchangeForToken)}(): {{@e}}", e);
      return null;
    }
  }

  public async Task<bool> IsAuthenticatedAsync() => await IsAuthenticatedAsync(GetCachedCookies(null));

  private async Task<bool> IsAuthenticatedAsync(CookieContainer cookies)
  { 
    var client = await GetAuthenticatedClient(cookies, withAuthToken: false);

    if (client is null) 
    {
      IsSignedIn = false;
      return false;
    }

    // Check login
    var res = await client.GetAsync("https://connect.garmin.com/modern/activities");
    IsSignedIn = !res.RequestMessage.RequestUri.ToString().StartsWith("https://connect.garmin.com/signin");
    return IsSignedIn;
  }

  /// <inheritdoc />
  /// <summary>
  /// Downloads the activity file.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="fileFormat">The file format.</param>
  /// </returns>
  public async Task<byte[]> DownloadActivityFile(long activityId, ActivityFileType fileFormat)
  {
    using HttpClient client = await GetAuthenticatedClient();

    var url = fileFormat == DefaultFile
        ? string.Format(UrlActivityDownloadDefaultFile, activityId)
        : string.Format(UrlActivityDownloadFile, fileFormat.ToString().ToLower(), activityId);

    var res = await client.GetAsync(url);

    return res.StatusCode == HttpStatusCode.OK 
      ? await res.Content.ReadAsByteArrayAsync() 
      : Array.Empty<byte>();
  }

  public async Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat)
  {
    using var stream = new FileStream(fileName, FileMode.Open);
    return await UploadActivity(stream, fileFormat).AnyContext();
  }

  public async Task<(bool Success, long ActivityId)> UploadActivity(Stream stream, FileFormat fileFormat)
  { 
    using HttpClient client = await GetAuthenticatedClient();

    var extension = fileFormat.FormatKey;
    string fileName = $"upload.{extension}";
    var url = $"{URL_UPLOAD}/.{extension}";

    var form = new MultipartFormDataContent($"---------------------------");

    using var content = new StreamContent(stream);

    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
    {
      Name = "file",
      FileName = Path.GetFileName(fileName),
      Size = stream.Length
    };

    form.Add(content, "file", Path.GetFileName(fileName));

    var res = await client.PostAsync(url, form);
    var responseData = await res.Content.ReadAsStringAsync();

    DetailedImportResponse response = null;

    try
    {
      response = JsonSerializer.Deserialize<DetailedImportResponse>(responseData);
    }
    catch (Exception e)
    {
      log_.LogError("Could not parse upload response: {responseData} {e}", responseData, e);
      return (false, -1);
    }

    if (!new HashSet<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.Conflict }
        .Contains(res.StatusCode))
    {
      log_.LogError("Failed to upload {@fileName}. Detail: {@responseData}", fileName, response);
      return (false, -1);
    }

    Success success = response.DetailedImportResult.Successes.FirstOrDefault();

    int id = success is null ? -1 : success.InternalId;
    return (true, id);
  }

  /// <inheritdoc />
  /// <summary>
  /// Sets the name of the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="activityName">Name of the activity.</param>
  /// <returns>
  /// The task
  /// </returns>
  public async Task<bool> SetActivityName(long activityId, string activityName)
  {
    using HttpClient client = await GetAuthenticatedClient();

    client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");
    client.DefaultRequestHeaders.Add("referer", "https://connect.garmin.com/modern");
    client.DefaultRequestHeaders.Add("Host", "connect.garmin.com");
    client.DefaultRequestHeaders.Add("Origin", "https://connect.garmin.com");
    
    var data = new
    {
      activityId,
      activityName
    };

    var url = $"{URL_ACTIVITY_BASE}/{activityId}";

    var res = await client.PostAsync(url,
        new StringContent(JsonSerializer.Serialize(data), new MediaTypeHeaderValue("application/json")));

    if (!res.IsSuccessStatusCode)
    {
      log_.LogError("Activity name not set: {@error}", await res.Content.ReadAsStringAsync());
      return false;
    }

    return true;
  }

  public async Task<GarminFitnessStats> GetLifetimeFitnessStats(CancellationToken ct = default) =>
    (await ExecuteUrlGetRequest<List<GarminFitnessStats>>($"{UrlFitnessStats}" +
         $"?aggregation=lifetime"
       + $"&startDate={new DateTime(1970, 1, 1):yyyy-MM-dd}"
       + $"&endDate={DateTime.Today:yyyy-MM-dd}"
       + $"&metric=duration"
       + $"&metric=distance"
       + $"&metric=movingDuration",
      "Error while getting lifetime fitness stats", ct))?.FirstOrDefault();

  public async Task<List<GarminFitnessStats>> GetYearyFitnessStats(CancellationToken ct = default) => 
    await ExecuteUrlGetRequest<List<GarminFitnessStats>>($"{UrlFitnessStats}" +
         $"?aggregation=yearly"
       + $"&startDate={new DateTime(1970, 1, 1):yyyy-MM-dd}"
       + $"&endDate={DateTime.Today:yyyy-MM-dd}"
       + $"&metric=duration"
       + $"&metric=distance"
       + $"&metric=movingDuration",
      "Error while getting yearly fitness stats", ct);

  /// <inheritdoc />
  /// <summary>
  /// Loads the activity types.
  /// </summary>
  /// <returns>
  /// List of activities
  /// </returns>
  public async Task<List<ActivityType>> LoadActivityTypes()
  {
    return await ExecuteUrlGetRequest<List<ActivityType>>(UrlActivityTypes,
        "Error while getting activity types");
  }

  /// <summary>
  /// Loads the event types.
  /// </summary>
  /// <returns></returns>
  public async Task<List<ActivityType>> LoadEventTypes()
  {
    return await ExecuteUrlGetRequest<List<ActivityType>>(UrlEventTypes,
        "Error while getting event types");
  }

  /// <inheritdoc />
  /// <summary>
  /// Sets the type of the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="activityType">Type of the activity.</param>
  /// <returns>
  /// The task
  /// </returns>
  public async Task<bool> SetActivityType(long activityId, ActivityType activityType)
  {
    using HttpClient client = await GetAuthenticatedClient();

    client.DefaultRequestHeaders.Remove("X-HTTP-Method-Override");
    client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

    var data = new
    {
      activityId,
      activityTypeDTO = activityType
    };

    var url = $"{URL_ACTIVITY_BASE}/{activityId}";
    var res = await client.PostAsync(url,
        new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));

    if (!res.IsSuccessStatusCode)
    {
      log_.LogError("Activity type not set: {@error}", await res.Content.ReadAsStringAsync());
      return false;
    }

    return true;
  }

  /// <summary>
  /// Sets the type of the event.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="eventType">Type of the event.</param>
  /// <returns></returns>
  public async Task<bool> SetEventType(long activityId, ActivityType eventType)
  {
    using HttpClient client = await GetAuthenticatedClient();

    client.DefaultRequestHeaders.Remove("X-HTTP-Method-Override");
    client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

    var data = new
    {
      activityId,
      eventTypeDTO = eventType
    };

    var url = $"{URL_ACTIVITY_BASE}/{activityId}";
    var res = await client.PostAsync(url,
        new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));

    if (!res.IsSuccessStatusCode)
    {
      log_.LogError("Event type not set: {@error}", await res.Content.ReadAsStringAsync());
      return false;
    }

    return true;
  }

  /// <inheritdoc />
  /// <summary>
  /// Sets the activity description.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="description">The description.</param>
  /// <returns>
  /// The task
  /// </returns>
  public async Task<bool> SetActivityDescription(long activityId, string description)
  {
    using HttpClient client = await GetAuthenticatedClient();

    client.DefaultRequestHeaders.Remove("X-HTTP-Method-Override");
    client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

    var data = new
    {
      activityId,
      description
    };

    var url = $"{URL_ACTIVITY_BASE}/{activityId}";
    var res = await client.PostAsync(url,
        new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));

    if (!res.IsSuccessStatusCode)
    {
      log_.LogError("Activity description not set: {@error}", await res.Content.ReadAsStringAsync());
      return false;
    }

    return true;
  }

  public async Task<bool> DeleteActivity(long activityId)
  {
    using HttpClient client = await GetAuthenticatedClient();

    client.DefaultRequestHeaders.Remove("X-HTTP-Method-Override");
    client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "DELETE");

    var url = $"{URL_ACTIVITY_BASE}/{activityId}";
    var res = await client.PostAsync(url, null);

    if (!res.IsSuccessStatusCode)
    {
      log_.LogError("Activity not deleted: {@error}", await res.Content.ReadAsStringAsync());
      return false;
    }

    return true;
  }

  /// <inheritdoc />
  /// <summary>
  /// Loads the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <returns>
  /// Activity
  /// </returns>
  public async Task<GarminActivity> LoadActivity(long activityId)
  {
    var url = $"{URL_ACTIVITY_BASE}/{activityId}";

    return await ExecuteUrlGetRequest<GarminActivity>(url, "Error while getting activity");
  }

  private static string CreateActivitiesUrl(int limit, int start, DateTime after, DateTime before)
  {
    return $"{UrlActivitiesBase}?limit={limit}&start={start}&startDate={after:yyyy-MM-dd}&endDate={before:yyyy-MM-dd}";
  }

  public async Task<List<GarminActivity>> LoadActivities(int limit, int start, DateTime after, DateTime before, CancellationToken ct = default)
  {
    var url = CreateActivitiesUrl(limit, start, after, before);

    return await ExecuteUrlGetRequest<List<GarminActivity>>(url, "Error while getting activities", ct);
  }

  private static T DeserializeData<T>(string data) where T : class
  {
    return typeof(T) == typeof(string) ? data as T : JsonSerializer.Deserialize<T>(data);
  }

  /// <summary>
  /// Executes the URL get request.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="url">The URL.</param>
  /// <param name="errorMessage">The error message.</param>
  /// <returns></returns>
  private async Task<T> ExecuteUrlGetRequest<T>(string url, string errorMessage, CancellationToken ct = default) where T : class
  {
    using HttpClient client = await GetAuthenticatedClient(ct: ct);
    var res = await client.GetAsync(url, ct);
    var data = await res.Content.ReadAsStringAsync(ct);
    if (!res.IsSuccessStatusCode)
    {
      log_.LogError($"{errorMessage}: {data} (HTTP {res.StatusCode})");
      return null;
    }

    return DeserializeData<T>(data);
  }
}
