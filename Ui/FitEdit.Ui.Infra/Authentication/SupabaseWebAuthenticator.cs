using System.Text.Json;
using System.Text.RegularExpressions;
using FitEdit.Model.Abstractions;
using FitEdit.Model.Clients;
using FitEdit.Ui.Infra.Supabase;
using FitEdit.Ui.Model.Supabase;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Supabase.Gotrue.Exceptions;

namespace FitEdit.Ui.Infra.Authentication;

public partial class SupabaseWebAuthenticator : ReactiveObject, IWebAuthenticator
{
  [GeneratedRegex("tester-(\\w+)@fitedit.io")]
  private static partial Regex TestAccountRegex();

  private static readonly string defaultUsername_ = "Please provide an email address";
  private readonly ILogger<SupabaseWebAuthenticator> log_;

  private readonly IFitEditClient fitEdit_;
  private readonly ISupabaseAdapter supa_;
  private readonly ITcpService tcp_;

  [Reactive] public string? Username { get; set; } = defaultUsername_;
  [Reactive] public bool IsAuthenticated { get; set; }

  public SupabaseWebAuthenticator(
    ILogger<SupabaseWebAuthenticator> log,
    IFitEditClient fitEdit,
    ISupabaseAdapter supa,
    ITcpService tcp
  )
  {
    log_ = log;
    fitEdit_ = fitEdit;
    supa_ = supa;
    tcp_ = tcp;
    supa_.ObservableForProperty(x => x.IsAuthenticated).Subscribe(async _ => await GetIsAuthenticatedAsync());
  }

  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    if (Username == null) { return false; }
    string username = Username;

    log_.LogInformation("Authenticating Supabase user for email=\'{@email}\'", Username);

    // First try to contact the API with existing token
    if (await GetIsAuthenticatedAsync(ct)) { return true; }

    try
    {
      Regex regex = TestAccountRegex();
      Match match = regex.Match(username);
      bool isTestAccount = match.Success && match.Groups.Count > 1;
      if (isTestAccount)
      {
        string password = match.Groups[1].Value;
        return await supa_.SignInWithEmailAndPassword(username, password, ct);
      }
      return await AuthenticateClientSideAsync(username, ct)
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

  public async Task<bool> AuthenticateClientSideAsync(string username, CancellationToken ct = default)
  {
    int port = tcp_.GetRandomUnusedPort();

    // Supabase is designed for web apps.
    // PKCE redirects the access token in the URL fragment (after a #) instead of in the query string (after a ?)
    // which prevents us from getting the access token since fragments don't leave the browser.
    bool usePkce = false;

    string? pkceVerifier = await supa_.SignInWithOtp(username, usePkce, $"http://localhost:{port}/auth/callback");

    var content = await new LoginRedirectContent().LoadContentAsync(ct);
    using var listener = new LoopbackHttpListener(content.SuccessHtml, content.ErrorHtml, port);

    FitEdit.Model.Authorization? auth = new();

    if (!usePkce)
    {
      try
      {
        string json = await listener.WaitForCallbackAsync(ct);
        auth = JsonSerializer.Deserialize<FitEdit.Model.Authorization>(json);
      }
      catch (TaskCanceledException)
      {
      }
    }

    else if (usePkce && pkceVerifier != null)
    {
      string? result = await listener.WaitForCallbackAsync(ct);
      if (ct.IsCancellationRequested) { return false; }

      // Get the auth code from the query string
      var uri = new Uri($"http://localhost:{port}/{result}");
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

    supa_.Authorization = AuthorizationFactory.Create(auth.AccessToken, auth.RefreshToken);
    return true;
  }

  private async Task<bool> GetIsAuthenticatedAsync(CancellationToken ct = default)
  {
    try
    {
      bool isAuthenticated = await supa_.IsAuthenticatedAsync(ct)
         && await fitEdit_.IsAuthenticatedAsync(ct);

      IsAuthenticated = isAuthenticated;
      Username = isAuthenticated
        ? supa_.Authorization?.Username ?? defaultUsername_
        : defaultUsername_;

      if (isAuthenticated)
      {
        FitEdit.Model.Log.Info("Successfully authenticated");
        return true;
      }

      FitEdit.Model.Log.Error($"Not authenticated");
      return false;
    }
    catch (Exception e)
    {
      FitEdit.Model.Log.Error($"Not authenticated: Exception: {e}");
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