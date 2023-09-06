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

public class GarminConnectClient : ReactiveObject, IGarminConnectClient
{
  private const string LOCALE = "en_US";
  private const string USER_AGENT = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:64.0) Gecko/20100101 Firefox/64.0";
  private const string CONNECT_DNS = "connect.garmin.com";
  private const string CONNECT_URL = "https://" + CONNECT_DNS;
  private const string CONNECT_URL_MODERN = CONNECT_URL + "/";
  private const string CONNECT_URL_SIGNIN = CONNECT_URL + "/signin/";
  private const string SSO_DNS = "sso.garmin.com";
  private const string SSO_URL = "https://" + SSO_DNS;
  private const string SSO_URL_SSO = SSO_URL + "/sso";
  private const string SSO_URL_SSO_SIGNIN = SSO_URL_SSO + "/signin";
  private const string CONNECT_URL_PROFILE = CONNECT_URL_MODERN + "proxy/userprofile-service/socialProfile/";
  private const string CONNECT_MODERN_HOSTNAME = "https://connect.garmin.com/modern/auth/hostname";
  private const string CSS_URL = CONNECT_URL + "/gauth-custom-v1.2-min.css";
  private const string PRIVACY_STATEMENT_URL = "https://www.garmin.com/en-US/privacy/connect/";
  private const string URL_UPLOAD = CONNECT_URL + "/proxy/upload-service/upload";
  private const string URL_ACTIVITY_BASE = CONNECT_URL + "/modern/proxy/activity-service/activity";

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
    {"connectLegalTerms", "true"},
    {"consumeServiceTicket", "false"},
    {"createAccountShown", "true"},
    {"cssUrl", CSS_URL},
    {"displayNameShown", "false"},
    {"embedWidget", "false"},
    // ReSharper disable once StringLiteralTypo
    {"gauthHost", SSO_URL_SSO},
    {"generateExtraServiceTicket", "true"},
    {"generateTwoExtraServiceTickets", "false"},
    {"generateNoServiceTicket", "false"},
    {"globalOptInChecked", "false"},
    {"globalOptInShown", "true"},
    // ReSharper disable once StringLiteralTypo
    {"id", "gauth-widget"},
    {"initialFocus", "true"},
    {"locale", LOCALE},
    {"locationPromptShon", "true"},
    {"mobile", "false"},
    {"openCreateAccount", "false"},
    {"privacyStatementUrl", PRIVACY_STATEMENT_URL},
    {"redirectAfterAccountCreationUrl", CONNECT_URL_MODERN},
    {"redirectAfterAccountLoginUrl", CONNECT_URL_MODERN},
    {"rememberMeChecked", "false"},
    {"rememberMeShown", "true"},
    {"service", CONNECT_URL_MODERN},
    {"showTermsOfUse", "false"},
    {"showPrivacyPolicy", "false"},
    {"showConnectLegalAge", "false"},
    {"showPassword", "true"},
    {"source", CONNECT_URL_SIGNIN},
    {"useCustomHeader", "false"},
    {"webhost", CONNECT_URL_MODERN}
  };

  /// <summary>
  /// The logger
  /// </summary>
  // ReSharper disable once NotAccessedField.Local
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

    return await EnsureTokenAsync(client) ? client : null;
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
    const double nsteps = 6.0;
    AuthenticateProgress = 0 / nsteps * 100;
    IsSignedIn = false;

    CookieContainer cookies = GetCachedCookies();
    HttpClient client = GetUnauthenticatedClient(cookies);

    var data = await client.GetStringAsync(CONNECT_MODERN_HOSTNAME);
    AuthenticateProgress = 1 / nsteps * 100;

    // data = {"host": "https://connect.garmin.com"}
    GarminHost host = JsonSerializer.Deserialize<GarminHost>(data);
    if (host?.Host is null)
    {
      log_.LogError("SSO hostname is missing");
      return false;
    }

    var queryParams = string.Join("&", QueryParams.Select(e => $"{e.Key}={WebUtility.UrlEncode(e.Value)}"));

    var url = $"{SSO_URL_SSO_SIGNIN}?{queryParams}";
    var res = await client.GetAsync(url);
    AuthenticateProgress = 2 / nsteps * 100;
    if (!ValidateResponseMessage(res, "No login form."))
    {
      return false;
    }

    data = await res.Content.ReadAsStringAsync();
    AuthenticateProgress = 3 / nsteps * 100;

    var csrfToken = "";
    try
    {
      csrfToken = GetValueByPattern(data, @"input type=\""hidden\"" name=\""_csrf\"" value=\""(\w+)\"" \/>", 2, 1);
    }
    catch (Exception e)
    {
      log_.LogError("Exception finding token by pattern: ", e);
      log_.LogError("data:\n", data);
      throw;
    }

    client.DefaultRequestHeaders.Add("origin", SSO_URL);
    client.DefaultRequestHeaders.Add("referer", url);
    client.DefaultRequestHeaders.Add(BaseHeader.Item1, BaseHeader.Item2);

    var formContent = new FormUrlEncodedContent(new[]
    {
      new KeyValuePair<string, string>("embed", "false"),
      new KeyValuePair<string, string>("username", Config.Username),
      new KeyValuePair<string, string>("password", Config.Password),
      new KeyValuePair<string, string>("_csrf", csrfToken)
    });

    res = await client.PostAsync(url, formContent);
    data = await res.Content.ReadAsStringAsync();
    AuthenticateProgress = 4 / nsteps * 100;

    if (!ValidateResponseMessage(res, $"Bad response {res.StatusCode}, expected {HttpStatusCode.OK}"))
    {
      return false;
    }

    if (!ValidateCookiePresence(cookies, "GARMIN-SSO-GUID"))
    {
      return false;
    }

    var ticket = GetValueByPattern(data, @"var response_url(\s+)= (\""|\').*?ticket=([\w\-]+)(\""|\')", 5, 3);

    // Second auth step
    // Needs a service ticket from previous response
    client.DefaultRequestHeaders.Remove("origin");
    url = $"{CONNECT_URL_MODERN}?ticket={WebUtility.UrlEncode(ticket)}";
    res = await client.GetAsync(url);
    AuthenticateProgress = 5 / nsteps * 100;

    if (!ValidateModernTicketUrlResponseMessage(res, $"Second auth step failed to produce success or expected 302: {res.StatusCode}."))
    {
      return false;
    }

    Cookies = cookies.MapModel();
    bool isAuthenticated = await IsAuthenticatedAsync();
    AuthenticateProgress = 6 / nsteps * 100;
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
    if (!ValidateCookiePresence(cookies, "SESSIONID"))
    {
      return false;
    }

    var client = await GetAuthenticatedClient(cookies);

    // Check login
    var res = await client.GetAsync(CONNECT_URL_PROFILE);
    IsSignedIn = ValidateResponseMessage(res, "Login check failed.");
    return IsSignedIn;
  }

  /// <summary>
  /// Gets the value by pattern.
  /// </summary>
  /// <param name="data">The data.</param>
  /// <param name="pattern">The pattern.</param>
  /// <param name="expectedCountOfGroups">The expected count of groups.</param>
  /// <param name="groupPosition">The group position.</param>
  /// <returns>Value of particular match group.</returns>
  /// <exception cref="Exception">Could not match expected pattern {pattern}</exception>
  private static string GetValueByPattern(string data, string pattern, int expectedCountOfGroups, int groupPosition)
  {
    var regex = new Regex(pattern);
    var match = regex.Match(data);
    if (!match.Success || match.Groups.Count != expectedCountOfGroups)
    {
      throw new Exception($"Could not match expected pattern {pattern}.");
    }
    return match.Groups[groupPosition].Value;
  }

  /// <summary>
  /// Validates the cookie presence.
  /// </summary>
  /// <param name="container">The container.</param>
  /// <param name="cookieName">Name of the cookie.</param>
  /// <exception cref="Exception">Missing cookie {cookieName}</exception>
  private bool ValidateCookiePresence(CookieContainer container, string cookieName)
  {
    var cookies = container.GetCookies(new Uri(CONNECT_URL_MODERN)).Cast<System.Net.Cookie>().ToList();
    System.Net.Cookie cookie = cookies.Find(e => string.Equals(cookieName, e.Name, StringComparison.InvariantCultureIgnoreCase));
    if (cookie is null)
    {
      log_.LogError("Missing Garmin cookie {@cookieName}", cookieName);
      return false;
    }

    if (cookie.Expired)
    {
      log_.LogError("Expird Garmin cookie {@cookieName}", cookieName);
      return false;
    }

    return true;
  }

  // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
  private bool ValidateResponseMessage(HttpResponseMessage responseMessage, string errorMessage)
  {
    if (!responseMessage.IsSuccessStatusCode)
    {
      log_.LogError(errorMessage);
      return false;
    }
    return true;
  }

  private bool ValidateModernTicketUrlResponseMessage(HttpResponseMessage responseMessage, string error)
  {
    if (!responseMessage.IsSuccessStatusCode && !responseMessage.StatusCode.Equals(HttpStatusCode.OK))
    {
      log_.LogError(error);
      return false;
    }
    return true;
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

    var form = new MultipartFormDataContent(
        $"------WebKitFormBoundary{DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}");

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
  /// Gets the unix timestamp.
  /// </summary>
  /// <param name="date">The date.</param>
  /// <returns></returns>
  private static int GetUnixTimestamp(DateTime date)
  {
    return (int)date.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
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
    return $"{UrlActivitiesBase}?limit={limit}&start={start}&_={GetUnixTimestamp(date)}";
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
