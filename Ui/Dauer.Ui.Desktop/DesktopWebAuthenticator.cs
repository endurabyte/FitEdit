using Dauer.Model.Data;
using Dauer.Ui.Desktop.Oidc;
using Dauer.Ui.Infra;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace Dauer.Ui.Desktop;

public class DesktopWebAuthenticator : ReactiveObject, IWebAuthenticator
{
  private readonly string redirectUri_ = $"https://www.fitedit.io/login-redirect.html";
  private string LogoutUri_ => $"https://auth3.fitedit.io/logout" +
                  $"?client_id={appClientId_}" +
                  $"&logout_uri=https://www.fitedit.io/";
  private string Authority_ => $"https://cognito-idp.us-east-1.amazonaws.com/{userPoolId_}";
  private readonly string userPoolId_ = "us-east-1_c6hFaUlt0";
  private readonly string appClientId_ = "30vleui8j8qe52hfd6k55mvdmr";
  private readonly IDatabaseAdapter db_;
  private readonly ILoggerFactory factory_;
  private readonly IFitEditService fitEdit_;
  private readonly Dauer.Model.Authorization auth_ = new() { Id = "Dauer.Api" };
  private static readonly string defaultUsername_ = "(Please log in)";
  private CancellationTokenSource refreshCts_ = new();

  private OidcClient? oidcClient_;

  [Reactive] public string? Username { get; set; } = defaultUsername_;
  [Reactive] public bool IsAuthenticated { get; set; }

  public DesktopWebAuthenticator(IDatabaseAdapter db, ILoggerFactory factory, IFitEditService fitEdit)
  {
    db_ = db;
    factory_ = factory;
    fitEdit_ = fitEdit;
    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ => await LoadCachedAuthorization());

    _ = Task.Run(InitAsync);
  }

  private async Task LoadCachedAuthorization()
  {
    if (db_ == null) { return; }
    if (!db_.Ready) { return; }

    Dauer.Model.Authorization result = await db_.GetAuthorizationAsync(auth_.Id);
    if (result == null) { return; }

    auth_.AccessToken = result.AccessToken;
    auth_.RefreshToken = result.RefreshToken;
    auth_.IdentityToken = result.IdentityToken;
    auth_.Expiry = result.Expiry;
    auth_.Username = result.Username;
    Username = result.Username ?? defaultUsername_;
    await AuthenticateAsync();
  }

  private void InitAsync()
  {
    var browser = new DesktopBrowser();

    var options = new OidcClientOptions
    {
      Authority = Authority_,
      ClientId = appClientId_,
      RedirectUri = redirectUri_,
      Scope = "email openid",
      FilterClaims = false,
      Browser = browser,

      // JS on the redirect page will split the state on the .
      // to get the local listen port
      AdditionalState = $".port={browser.Port}"
    };
    options.Policy.Discovery.ValidateEndpoints = false;
    options.LoggerFactory = factory_;

    oidcClient_ = new OidcClient(options);
  }

  public async Task<bool> LogoutAsync(CancellationToken ct = default)
  {
    if (oidcClient_ == null) { IsAuthenticated = false; return false; }

    try
    {
      // Cognito OpenID connect discovery does not support logout: https://stackoverflow.com/a/56221548/16246783
      // Cognito is not even OpenID certified: https://openid.net/certification/
      //var req = new LogoutRequest { IdTokenHint = auth_.IdentityToken, }
      //LogoutResult result = await oidcClient_.LogoutAsync(req, ct);

      var client = new HttpClient();
      HttpResponseMessage resp = await client.GetAsync(LogoutUri_, ct);
      bool ok = resp.IsSuccessStatusCode;

      // No need to open the browser
      //BrowserResult? result = await oidcClient_.Options.Browser
      //  .InvokeAsync(new BrowserOptions(uri, "https://www.fitedit.io/") { Timeout = TimeSpan.Zero, });
      //bool ok = !result.IsError;

      if (!ok) 
      {
        string content = await resp.Content.ReadAsStringAsync(ct);
        Log.Error($"Logout error: {content}");
        return false; 
      }

      Log.Information("Logged out");
      IsAuthenticated = false;
      Username = defaultUsername_;
      auth_.IdentityToken = null;
      auth_.AccessToken = null;
      auth_.RefreshToken = null;
      auth_.Username = null;
      await db_.InsertAsync(auth_);

      return true;
    }
    catch (Exception e)
    {
      Log.Error($"{e}");
      return false;
    }
  }

  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    StopRefreshLoop();

    try
    {
      // First try to contact the API with existing token
      if (await GetIsAuthenticated(auth_.AccessToken, ct)) { IsAuthenticated = true; return true; }

      // Not authorized to use the API. Try to refresh the token
      if (await RefreshTokenAsync(auth_.RefreshToken, ct)) { IsAuthenticated = true; return true; }

      // Refresh token didn't work. We need to reuthenticate
      if (oidcClient_ == null) { IsAuthenticated = false; return false; }
      LoginResult result = await oidcClient_.LoginAsync(cancellationToken: ct);

      if (!await TryParse(result)) { IsAuthenticated = false; return false; }

      IsAuthenticated = true;
      return true;
    }
    catch (Exception e)
    {
      Log.Error($"{e}");
      return false;
    }
    finally
    {
      StartRefreshLoop();
    }
  }

  /// <summary>
  /// Stop the automatic refresh loop (no-op if it's not running)
  /// </summary>
  private void StopRefreshLoop()
  {
    refreshCts_.Cancel();
    refreshCts_ = new();
  }

  private void StartRefreshLoop()
  {
    CancellationToken refreshCt = refreshCts_.Token;
    _ = Task.Run(async () => await RunRefreshLoop(refreshCt), refreshCt);
  }

  /// <summary>
  /// Run a loop that refreshes the access token just before expiration.
  /// </summary>
  private async Task RunRefreshLoop(CancellationToken ct = default)
  {
    var margin = TimeSpan.FromMinutes(5); // e.g. refresh 5 minutes before expiry

    while (!ct.IsCancellationRequested)
    {
      // Run an initial check before delaying for a long time
      if (!await GetIsAuthenticated(auth_.AccessToken, ct)) { return; }

      TimeSpan delay = auth_.Expiry - DateTimeOffset.UtcNow - margin;
      await Task.Delay(delay, ct);

      // The token is almost expired. Are we still authenticated? We need it to refresh.
      // If not, reauthenticate and exit the loop. On reauth, a new loop will be started.
      if (!await GetIsAuthenticated(auth_.AccessToken, ct)) { await AuthenticateAsync(ct); return; }

      // Could we refresh the access token?
      // If not, reauthenticate and exit the loop. On reauth, a new loop will be started.
      if (!await RefreshTokenAsync(auth_.RefreshToken, ct)) { await AuthenticateAsync(ct); return; }
    }
  }

  private async Task<bool> RefreshTokenAsync(string? refreshToken, CancellationToken ct = default)
  {
    Dauer.Model.Log.Info($"{nameof(DesktopWebAuthenticator)}.{nameof(RefreshTokenAsync)}({refreshToken}, ...)");

    if (refreshToken == null) { return false; }
    if (oidcClient_ == null) { return false; }

    var result = await oidcClient_.RefreshTokenAsync(refreshToken, cancellationToken: ct);
    return await TryParse(result);
  }

  private async Task<bool> TryParse(LoginResult result)
  {
    if (result.IsError)
    {
      Dauer.Model.Log.Error($"Login error: {result.Error}: {result.ErrorDescription}");
      return false;
    }

    Dauer.Model.Log.Info($"LoginResult:");

    Dauer.Model.Log.Info("Claims:");
    foreach (var claim in result.User.Claims)
    {
      Dauer.Model.Log.Info($"{claim.Type}: {claim.Value}");
      if (claim?.Type == "email")
      {
        Username = claim.Value;
        auth_.Username = claim.Value;
      }
    }

    Dauer.Model.Log.Info($"Identity token: {result.IdentityToken}");
    Dauer.Model.Log.Info($"Access token:   {result.AccessToken}");
    Dauer.Model.Log.Info($"  Expires:  {result.AccessTokenExpiration}");
    Dauer.Model.Log.Info($"Refresh token:  {result.RefreshToken}");

    auth_.IdentityToken = result.IdentityToken;
    auth_.AccessToken = result.AccessToken;
    auth_.Expiry = result.AccessTokenExpiration;
    auth_.RefreshToken = result.RefreshToken;
    await db_.InsertAsync(auth_);

    return true;
  }

  private async Task<bool> TryParse(RefreshTokenResult result)
  {
    if (result.IsError)
    {
      Dauer.Model.Log.Error($"Refresh token error: {result.Error}: {result.ErrorDescription}");
      return false;
    }

    Dauer.Model.Log.Info($"RefreshTokenResult:");
    Dauer.Model.Log.Info($"Identity token: {result.IdentityToken}");
    Dauer.Model.Log.Info($"Access token:   {result.AccessToken}");
    Dauer.Model.Log.Info($"  Expires:  {result.AccessTokenExpiration}");
    Dauer.Model.Log.Info($"Refresh token:  {result.RefreshToken}");

    auth_.IdentityToken = result.IdentityToken;
    auth_.AccessToken = result.AccessToken;
    auth_.Expiry = result.AccessTokenExpiration;
    await db_.InsertAsync(auth_);

    return true;
  }

  private async Task<bool> GetIsAuthenticated(string? accessToken, CancellationToken ct = default)
  {
    if (accessToken == null) { return false; }

    bool isAuthenticated = await fitEdit_.IsAuthenticatedAsync(ct);

    if (isAuthenticated)
    {
      Dauer.Model.Log.Info("Successfully authenticated");
      return true;
    }

    Dauer.Model.Log.Error($"Not authenticated");
    return false;
  }
}