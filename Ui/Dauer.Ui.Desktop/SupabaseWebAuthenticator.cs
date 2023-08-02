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
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using static Supabase.Functions.Client;
using static Supabase.Gotrue.Constants;

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
  private readonly Supabase.Client client_;
  private readonly ILogger<SupabaseWebAuthenticator> log_;

  private readonly IDatabaseAdapter db_;
  private Dauer.Model.Authorization auth_ = new() { Id = "Dauer.Api" };

  [Reactive] public string Username { get; set; } = defaultUsername_;
  [Reactive] public bool LoggedIn { get; set; }

  public string Email { get; set; } = "doug@slater.dev";

  public SupabaseWebAuthenticator(ILogger<SupabaseWebAuthenticator> log, IDatabaseAdapter db, string url, string key)
  {
    log_ = log;
    db_ = db;

    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ => await LoadCachedAuthorization());

    client_ = new Supabase.Client(url, key, new SupabaseOptions
    {

    });

    client_.Auth.AddDebugListener((message, exception) =>
    {
      log_.LogInformation("Auth debug: {@message} {@debug}", message, exception);
    });

    client_.Auth.AddStateChangedListener(async (sender, changed) =>
    {
      log_.LogInformation("Auth state changed to {@changed}", changed);

      switch (changed)
      {
        case AuthState.SignedIn:
          break;
        case AuthState.SignedOut:
          break;
        case AuthState.UserUpdated:
          break;
        case AuthState.PasswordRecovery:
          break;
        case AuthState.TokenRefreshed:
          break;
      }

      await GetIsAuthenticated(auth_.AccessToken);
    });

    _ = Task.Run(async () =>
    {
      await client_.InitializeAsync();
    });
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
      client_.Auth.SetAuth(auth_.AccessToken!);
    }

    await AuthenticateAsync();
  }

  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    if (Email == null) { return false; }
    log_.LogInformation("Authenticating Supabase user for email=\'{@email}\'", Email);

    // First try to contact the API with existing token
    if (await GetIsAuthenticated(auth_.AccessToken, ct)) { return true; }

    try
    {
      //return await AuthenticateClientSideAsync(Email, "supersecret", ct);
      return await AuthenticateClientSideAsync(Email, ct)
       && await GetIsAuthenticated(auth_.AccessToken, ct);
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

  public async Task<bool> AuthenticateClientSideAsync(string email, string password, CancellationToken ct = default)
  {
    await client_.Auth.ResetPasswordForEmail(email);
    Session? session = await client_.Auth.SignInWithPassword(email, password);

    bool ok = session?.AccessToken != null;
    return ok;
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

    PasswordlessSignInState? signInState = await client_.Auth.SignInWithOtp(new SignInWithPasswordlessEmailOptions(email)
    {
      EmailRedirectTo = $"http://localhost:{port}/auth/callback",
      FlowType = usePkce ? OAuthFlowType.PKCE : OAuthFlowType.Implicit,
    });

    var content = await new LoginRedirectContent().LoadContentAsync(ct);
    using var listener = new LoopbackHttpListener(content.SuccessHtml, content.ErrorHtml, port);

    Dauer.Model.Authorization? auth = new();

    if (!usePkce)
    {
      string json = await listener.WaitForCallbackAsync(ct);
      auth = JsonSerializer.Deserialize<Dauer.Model.Authorization>(json); 
    }

    else if (usePkce & signInState != null && signInState?.PKCEVerifier != null)
    {
      string? result = await listener.WaitForCallbackAsync(ct);
      var uri = new Uri($"http://localhost:{port}/{result}");

      // Get the auth code from the query string
      string? code = uri.Query.Split('?', '&').First(x => x.StartsWith("code="))[5..];
      Session? session = await client_.Auth.ExchangeCodeForSession(signInState.PKCEVerifier, code);

      auth.AccessToken = session?.AccessToken;
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

    client_.Auth.SetAuth(auth.AccessToken);

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

  private async Task<bool> GetIsAuthenticated(string? accessToken, CancellationToken ct = default)
  {
    if (accessToken == null) { return false; }

    Guid id = Guid.NewGuid();
    bool isAuthenticated = false;
    try
    {
      string json = await client_.Functions.Invoke("is-authorized", options: new InvokeFunctionOptions
      {
        Body = new Dictionary<string, object>
        {
          { "id", id },
        },
      });

      var response = JsonSerializer.Deserialize<IsAuthorizedResponse>(json);

      if (response?.Message == $"Hello {id}!")
      {
        Dauer.Model.Log.Info("Successfully authenticated");
        isAuthenticated = true;
      }

      Dauer.Model.Log.Error($"Not authenticated: 401 Unauthorized");
    }
    catch (Exception e)
    {
      Dauer.Model.Log.Error($"Not authenticated: Exception during GetIsAuthenticated: {e}");
    }

    LoggedIn = isAuthenticated;
    Username = isAuthenticated 
      ? auth_.Username ?? defaultUsername_ 
      : defaultUsername_;

    return isAuthenticated;
  }

  public async Task<bool> LogoutAsync(CancellationToken ct = default)
  {
    client_.Auth.SetAuth("");
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
