using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dauer.Model.Data;
using Dauer.Ui.Desktop.Oidc;
using Dauer.Ui.Infra;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Supabase.Gotrue.Exceptions;

namespace Dauer.Ui.Desktop;

public class IsAuthorizedResponse
{
  [JsonPropertyName("message")]
  public string? Message { get; set; }
}

public class SupabaseWebAuthenticator : ReactiveObject, IWebAuthenticator
{
  private static readonly string defaultUsername_ = "(Please log in)";
  private CancellationTokenSource listenCts_ = new();
  private readonly ILogger<SupabaseWebAuthenticator> log_;

  private readonly IDatabaseAdapter db_;
  private readonly IFitEditClient fitEdit_;
  private readonly ISupabaseAdapter supa_;
  private Dauer.Model.Authorization auth_ = new() { Id = "Dauer.Api" };

  [Reactive] public string Username { get; set; } = defaultUsername_;
  [Reactive] public bool LoggedIn { get; set; }

  public string Email { get; set; } = "doug@slater.dev";

  public SupabaseWebAuthenticator(
    ILogger<SupabaseWebAuthenticator> log,
    IDatabaseAdapter db,
    IFitEditClient fitEdit,
    ISupabaseAdapter supa
  )
  {
    log_ = log;
    db_ = db;
    fitEdit_ = fitEdit;
    supa_ = supa;

    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ => await LoadCachedAuthorization());
    supa_.ObservableForProperty(x => x.IsAuthenticated).Subscribe(async _ => await GetIsAuthenticatedAsync(auth_.AccessToken));
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

    if (result.AccessToken != null)
    {
      supa_.SetAccessToken(auth_.AccessToken);
    }

    await AuthenticateAsync();
  }

  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    if (Email == null) { return false; }
    log_.LogInformation("Authenticating Supabase user for email=\'{@email}\'", Email);

    // First try to contact the API with existing token
    if (await GetIsAuthenticatedAsync(auth_.AccessToken, ct)) { return true; }

    try
    {
      //return await AuthenticateClientSideAsync(Email, "supersecret", ct);
      return await AuthenticateClientSideAsync(Email, ct)
       && await GetIsAuthenticatedAsync(auth_.AccessToken, ct);
    }
    catch (GotrueException e)
    {
      log_.LogError("Gotrue exception authenticating: {@e}", e);
      return false;
    }
    catch (Exception e)
    {
      log_.LogError("Exception authenticating: {@e}", e);
      return false;
    }
  }

  public async Task<bool> AuthenticateClientSideAsync(string email, CancellationToken ct = default)
  {
    // 3 ways to cancel:
    //   this method gets called again (so we have our own CT)
    //   caller cancels the given CT (we link our CT to it),
    //   LoopbackHttpListener timeouts (based on its internal CT),
    listenCts_.Cancel();
    listenCts_ = new CancellationTokenSource();
    ct.Register(listenCts_.Cancel);

    int port = Tcp.GetRandomUnusedPort();

    bool usePkce = false;

    string? pkceVerifier = await supa_.SignInWithMagicLink(email, usePkce, $"http://localhost:{port}/auth/callback");

    var content = await new LoginRedirectContent().LoadContentAsync(ct);
    using var listener = new LoopbackHttpListener(content.SuccessHtml, content.ErrorHtml, port);

    Dauer.Model.Authorization? auth = new();

    if (!usePkce)
    {
      string json = await listener.WaitForCallbackAsync(ct);
      auth = JsonSerializer.Deserialize<Dauer.Model.Authorization>(json); 
    }

    else if (usePkce && pkceVerifier != null)
    {
      string? result = await listener.WaitForCallbackAsync(ct);
      var uri = new Uri($"http://localhost:{port}/{result}");

      // Get the auth code from the query string
      string? code = uri.Query.Split('?', '&').First(x => x.StartsWith("code="))[5..];

      auth.AccessToken = await supa_.ExchangeCodeForSession(pkceVerifier, code);
    }

    if (auth == null)
    {
      LoggedIn = false;
      Username = defaultUsername_;
      return false;
    }

    if (auth?.AccessToken == null)
    {
      LoggedIn = false;
      Username = defaultUsername_;
      return false;
    }

    supa_.SetAccessToken(auth.AccessToken);

    var token = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);
    var identity = new ClaimsPrincipal(new ClaimsIdentity(token.Claims));

    string? username = identity.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
    string? sub = identity.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
    long.TryParse(identity.Claims.FirstOrDefault(x => x.Type == "exp")?.Value, out long exp);
    var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);

    auth.Username = username;
    auth.IdentityToken = "";
    auth.Id = "Dauer.Api";
    auth.Expiry = expiry;

    await db_.InsertAsync(auth);

    auth_ = auth;
    return true;
  }

  private async Task<bool> GetIsAuthenticatedAsync(string? accessToken, CancellationToken ct = default)
  {
    try
    {
      bool isAuthenticated =
            await supa_.IsAuthenticatedAsync(accessToken, ct)
         && await fitEdit_.IsAuthenticatedAsync(auth_.AccessToken, ct);

      if (isAuthenticated)
      {
        Dauer.Model.Log.Info("Successfully authenticated");

        LoggedIn = isAuthenticated;
        Username = isAuthenticated
          ? auth_.Username ?? defaultUsername_
          : defaultUsername_;

        return true;
      }
    }
    catch (Exception e)
    {
      Dauer.Model.Log.Error($"Not authenticated: Exception: {e}");
    }

    Dauer.Model.Log.Error($"Not authenticated");
    return false;
  }

  public async Task<bool> LogoutAsync(CancellationToken ct = default)
  {
    supa_.SetAccessToken("");
    LoggedIn = false;
    Username = defaultUsername_;

    auth_.IdentityToken = null;
    auth_.AccessToken = null;
    auth_.RefreshToken = null;
    auth_.Username = null;
    await db_.InsertAsync(auth_);

    return true;
  }
}
