using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
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
  private static readonly string defaultUsername_ = "Please provide an email address";
  private CancellationTokenSource listenCts_ = new();
  private readonly ILogger<SupabaseWebAuthenticator> log_;

  private readonly IFitEditClient fitEdit_;
  private readonly ISupabaseAdapter supa_;

  [Reactive] public string? Username { get; set; } = defaultUsername_;
  [Reactive] public bool IsAuthenticated { get; set; }

  public SupabaseWebAuthenticator(
    ILogger<SupabaseWebAuthenticator> log,
    IFitEditClient fitEdit,
    ISupabaseAdapter supa
  )
  {
    log_ = log;
    fitEdit_ = fitEdit;
    supa_ = supa;

    supa_.ObservableForProperty(x => x.IsAuthenticated).Subscribe(async _ => await GetIsAuthenticatedAsync());
  }

  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    if (Username == null) { return false; }
    string email = Username;
    if (!EmailValidator.IsValid(email)) { return false; }

    log_.LogInformation("Authenticating Supabase user for email=\'{@email}\'", Username);

    // First try to contact the API with existing token
    if (await GetIsAuthenticatedAsync(ct)) { return true; }

    try
    {
      //return await AuthenticateClientSideAsync(Email, "supersecret", ct);
      return await AuthenticateClientSideAsync(email, ct)
       && await GetIsAuthenticatedAsync(ct);
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

    // Supabase is designed for web apps.
    // PKCE redirects the access token in the URL fragment (after a #) instead of in the query string (after a ?)
    // which prevents us from getting the access token since fragments don't leave the browser.
    bool usePkce = false;

    string? pkceVerifier = await supa_.AuthenticateWithMagicLink(email, usePkce, $"http://localhost:{port}/auth/callback");

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
      IsAuthenticated = false;
      Username = defaultUsername_;
      return false;
    }

    if (auth?.AccessToken == null)
    {
      IsAuthenticated = false;
      Username = defaultUsername_;
      return false;
    }

    var token = new JwtSecurityTokenHandler().ReadJwtToken(auth.AccessToken);
    var identity = new ClaimsPrincipal(new ClaimsIdentity(token.Claims));

    string? username = identity.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
    string? sub = identity.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
    long.TryParse(identity.Claims.FirstOrDefault(x => x.Type == "exp")?.Value, out long exp);
    long.TryParse(identity.Claims.FirstOrDefault(x => x.Type == "iat")?.Value, out long iat);
    var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iat);
    var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);

    auth.Username = username;
    auth.IdentityToken = "";
    auth.Id = "Dauer.Api";
    auth.Created = issuedAt;
    auth.Expiry = expiry;

    supa_.Authorization = new Dauer.Model.Authorization(auth);
    return true;
  }

  private async Task<bool> GetIsAuthenticatedAsync(CancellationToken ct = default)
  {
    try
    {
      bool isAuthenticated =
            await supa_.IsAuthenticatedAsync(ct)
         && await fitEdit_.IsAuthenticatedAsync(supa_.Authorization?.AccessToken, ct);

      IsAuthenticated = isAuthenticated;
      Username = isAuthenticated
        ? supa_.Authorization?.Username ?? defaultUsername_
        : defaultUsername_;

      if (isAuthenticated)
      {
        Dauer.Model.Log.Info("Successfully authenticated");
        return true;
      }

      Dauer.Model.Log.Error($"Not authenticated");
      return false;
    }
    catch (Exception e)
    {
      Dauer.Model.Log.Error($"Not authenticated: Exception: {e}");
      return false;
    }
  }

  public async Task<bool> LogoutAsync(CancellationToken ct = default)
  {
    await supa_.LogoutAsync();
    IsAuthenticated = false;

    await Task.CompletedTask;
    return true;
  }
}
