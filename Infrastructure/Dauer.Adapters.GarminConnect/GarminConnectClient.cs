using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.GarminConnect;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Adapters.GarminConnect;

public partial class GarminConnectClient : ReactiveObject, IGarminConnectClient
{
  private const string LOCALE = "en-US";
  private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/117.0";
  private const string SSO_URL = "https://sso.garmin.com";
  private const string SSO_LOGIN = SSO_URL + "/portal/api/login";
  private const string CONNECT_URL = "https://connect.garmin.com";
  private const string CONNECT_URL_MODERN = CONNECT_URL + "/modern";
  private const string CONNECT_MODERN_HOSTNAME = "https://connect.garmin.com/modern/auth/hostname";
  private const string URL_PROFILE = CONNECT_URL_MODERN + "/proxy/userprofile-service/socialProfile/";
  private const string URL_UPLOAD = CONNECT_URL + "/proxy/upload-service/upload";
  private const string URL_ACTIVITY_BASE = CONNECT_URL_MODERN + "/proxy/activity-service/activity";

  private const string UrlActivityTypes = "https://connect.garmin.com/proxy/activity-service/activity/activityTypes";
  private const string UrlEventTypes = "https://connect.garmin.com/proxy/activity-service/activity/eventTypes";
  private const string UrlActivitiesBase = "https://connect.garmin.com/proxy/activitylist-service/activities/search/activities";
  private const string UrlActivityDownloadFile = "https://connect.garmin.com/modern/proxy/download-service/export/{0}/activity/{1}";
  private const string UrlActivityDownloadDefaultFile = "https://connect.garmin.com/modern/proxy/download-service/files/activity/{0}";

  private const ActivityFileType DefaultFile = ActivityFileType.Fit;

  public Dictionary<string, Model.Cookie> Cookies { get; set; }
  private GarminAccessToken token_;

  private static readonly Tuple<string, string> BaseHeader = new("NK", "NT");

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

  public GarminConnectConfig Config { get; set; } = new();

  [Reactive] public double AuthenticateProgress { get; private set; }
  [Reactive] public bool IsSignedIn { get; set; }

  /// <summary>
  /// Initializes a new instance of the <see cref="GarminConnectClient"/> class.
  /// </summary>
  /// <param name="config">The configuration.</param>
  /// <param name="log">The logger.</param>
  public GarminConnectClient(ILogger<GarminConnectClient> log)
  {
    log_ = log;

    Cookies = new Dictionary<string, Model.Cookie>();
  }

  private CookieContainer GetCachedCookies() => Cookies.MapCookieContainer();

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

  private async Task<HttpClient> GetAuthenticatedClient(CookieContainer cookies = null)
  {
    var client = GetUnauthenticatedClient(cookies ?? GetCachedCookies());

    client.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);
    client.DefaultRequestHeaders.Add("origin", CONNECT_URL);
    client.DefaultRequestHeaders.Add("referer", $"{CONNECT_URL}/modern");
    client.DefaultRequestHeaders.Add("dnt", "1");

    // First try can fail. Try twice.
    return await EnsureTokenAsync(client) 
      ? client 
      : await EnsureTokenAsync(client)
       ? client
       : null;
  }

  /// <inheritdoc />
  /// <summary>
  /// Authenticates this instance.
  /// </summary>
  /// <returns>
  /// Tuple of Cookies and HTTP handler
  /// </returns>
  /// <exception cref="T:System.Exception">
  /// SSO hostname is missing
  /// or
  /// Could not match service ticket.
  /// </exception>
  public async Task<bool> AuthenticateAsync()
  {
    const double nsteps = 5.0;
    AuthenticateProgress = 0 / nsteps * 100;
    IsSignedIn = false;

    CookieContainer cookies = GetCachedCookies();
    using HttpClient client = GetUnauthenticatedClient(cookies);

    var data = await client.GetStringAsync(CONNECT_MODERN_HOSTNAME);
    AuthenticateProgress = 1 / nsteps * 100;

    // data = {"host": "https://connect.garmin.com"}
    GarminHost host = JsonSerializer.Deserialize<GarminHost>(data);
    if (host?.Host is null)
    {
      log_.LogError("SSO hostname is missing");
      return false;
    }

    data = await client.GetStringAsync(host.Host);
    AuthenticateProgress = 2 / nsteps * 100;

    var tmpCookies = cookies.MapModel();

    var dict = new Dictionary<string, object>
    {
      ["captchaToken"] = "",
      ["rememberMe"] = false,
      ["username"] = Config.Username,
      ["password"] = Config.Password,
    };

    var queryParams = string.Join("&", QueryParams.Select(e => $"{e.Key}={WebUtility.UrlEncode(e.Value)}"));

    client.DefaultRequestHeaders.Remove("dnt");
    client.DefaultRequestHeaders.Add("dnt", "1");
    client.DefaultRequestHeaders.Remove("referer");
    client.DefaultRequestHeaders.Add("referer", SSO_URL + $"/portal/sso/{LOCALE}/sign-in?{queryParams}");

    var url = $"{SSO_LOGIN}?{queryParams}";
    var res = await client.PostAsJsonAsync(url, dict);
    AuthenticateProgress = 3 / nsteps * 100;
    var loginString = await res.Content.ReadAsStringAsync();
    var loginResponse = await res.Content.ReadFromJsonAsync<GarminLoginResponse>();

    if (!res.RequireHttpOk($"POST {SSO_LOGIN} failed"))
    {
      log_.LogError("Login response: {@loginResponse}", loginResponse);
      return false;
    }

    if (loginResponse?.ServiceTicketId is null)
    {
      log_.LogError("No Garmin login ticket");
      return false;
    }

    client.DefaultRequestHeaders.Remove("referer");
    client.DefaultRequestHeaders.Add("referer", SSO_URL);

    // Second auth step
    // Needs a service ticket from previous response
    client.DefaultRequestHeaders.Remove("origin");
    url = $"{CONNECT_URL_MODERN}?ticket={WebUtility.UrlEncode(loginResponse.ServiceTicketId)}";
    res = await client.GetAsync(url);
    AuthenticateProgress = 4 / nsteps * 100;

    if (!res.RequireHttpOk200($"Second auth step failed to produce success or expected 302: {res.StatusCode}."))
    {
      return false;
    }

    Cookies = cookies.MapModel();
    bool isAuthenticated = await IsAuthenticatedAsync();
    AuthenticateProgress = 5 / nsteps * 100;
    return isAuthenticated;
  }

  public Task<bool> LogoutAsync()
  {
    Config = new();
    token_ = null;
    Cookies = new Dictionary<string, Model.Cookie>();
    IsSignedIn = false;
    return Task.FromResult(true);
  }

  private async Task<bool> EnsureTokenAsync(HttpClient client)
  {
    if (token_ is not null) 
    {
      client.DefaultRequestHeaders.Remove("authorization");
      client.DefaultRequestHeaders.Add("authorization", $"Bearer {token_.AccessToken}");
      return true; 
    }

    // Exchange for oauth token
    string url = $"{CONNECT_URL}/modern/di-oauth/exchange";

    try
    {
      HttpResponseMessage res = await client.PostAsync(url, null);

      if (!res.IsSuccessStatusCode)
      {
        return false;
      }

      token_ = await res.Content.ReadFromJsonAsync<GarminAccessToken>();
      client.DefaultRequestHeaders.Remove("Authorization");
      client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token_.AccessToken}");
      return token_.AccessToken is not null;
    }
    catch (Exception e)
    {
      log_.LogError($"{nameof(EnsureTokenAsync)}(): {e}", e);
      return false;
    }
  }

  public async Task<bool> IsAuthenticatedAsync() => await IsAuthenticatedAsync(GetCachedCookies());

  private async Task<bool> IsAuthenticatedAsync(CookieContainer cookies)
  { 
    // Check session cookie
    if (!cookies.ValidateCookiePresence("SESSIONID", CONNECT_URL_MODERN))
    {
      return false;
    }

    var client = await GetAuthenticatedClient(cookies);

    if (client is null) 
    {
      IsSignedIn = false;
      return false;
    }

    // Check login
    var res = await client.GetAsync(URL_PROFILE);
    IsSignedIn = res.RequireHttpOk("Login check failed.");
    return IsSignedIn;
  }

  /// <inheritdoc />
  /// <summary>
  /// Downloads the activity file.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="fileFormat">The file format.</param>
  /// <returns>
  /// Stream
  /// </returns>
  public async Task<Stream> DownloadActivityFile(long activityId, ActivityFileType fileFormat)
  {
    using HttpClient client = await GetAuthenticatedClient();

    var url = fileFormat == DefaultFile
        ? string.Format(UrlActivityDownloadDefaultFile, activityId)
        : string.Format(UrlActivityDownloadFile, fileFormat.ToString().ToLower(), activityId);

    Stream streamCopy = new MemoryStream();
    var res = await client.GetAsync(url);

    await (await res.Content.ReadAsStreamAsync()).CopyToAsync(streamCopy);
    return streamCopy;
  }

  public async Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat)
  {
    using var stream = new FileStream(fileName, FileMode.Open);
    return await UploadActivity(fileName, stream, fileFormat).AnyContext();
  }

  public async Task<(bool Success, long ActivityId)> UploadActivity(string fileName, Stream stream, FileFormat fileFormat)
  { 
    using HttpClient client = await GetAuthenticatedClient();

    var extension = fileFormat.FormatKey;
    var url = $"{URL_UPLOAD}/.{extension}";

    var form = new MultipartFormDataContent($"------WebKitFormBoundary{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}");

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
      log_.LogError("Could not parse upload response: {e}", e);
      return (false, -1);
    }

    if (!new HashSet<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.Conflict }
        .Contains(res.StatusCode))
    {
      log_.LogError("Failed to upload {@fileName}. Detail: {@responseData}", fileName, response);
      return (false, -1);
    }

    Success success = response.DetailImportResult.Successes.FirstOrDefault();

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

    client.DefaultRequestHeaders.Remove("X-HTTP-Method-Override");
    client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "PUT");

    var data = new
    {
      activityId,
      activityName
    };

    var url = $"{URL_ACTIVITY_BASE}/{activityId}";
    var res = await client.PostAsync(url,
        new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));

    if (!res.IsSuccessStatusCode)
    {
      log_.LogError("Activity name not set: {@error}", await res.Content.ReadAsStringAsync());
      return false;
    }

    return true;
  }

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
  public async Task<Activity> LoadActivity(long activityId)
  {
    var url = $"{URL_ACTIVITY_BASE}/{activityId}";

    return await ExecuteUrlGetRequest<Activity>(url, "Error while getting activity");
  }

  /// <summary>
  /// Creates the activities URL.
  /// </summary>
  /// <param name="limit">The limit.</param>
  /// <param name="start">The start.</param>
  /// <param name="date">The date.</param>
  /// <returns></returns>
  private static string CreateActivitiesUrl(int limit, int start, DateTime date)
  {
    return $"{UrlActivitiesBase}?limit={limit}&start={start}&_={date.GetUnixTimestamp()}";
  }

  /// <inheritdoc />
  /// <summary>
  /// Loads the activities.
  /// </summary>
  /// <param name="limit">The limit.</param>
  /// <param name="start">The start.</param>
  /// <param name="from">From.</param>
  /// <returns>
  /// List of activities
  /// </returns>
  public async Task<List<Activity>> LoadActivities(int limit, int start, DateTime from)
  {
    var url = CreateActivitiesUrl(limit, start, from);

    return await ExecuteUrlGetRequest<List<Activity>>(url, "Error while getting activities");
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
  private async Task<T> ExecuteUrlGetRequest<T>(string url, string errorMessage) where T : class
  {
    using HttpClient client = await GetAuthenticatedClient();
    var res = await client.GetAsync(url);
    var data = await res.Content.ReadAsStringAsync();
    if (!res.IsSuccessStatusCode)
    {
      throw new Exception($"{errorMessage}: {data}");
    }

    return DeserializeData<T>(data);
  }
}
