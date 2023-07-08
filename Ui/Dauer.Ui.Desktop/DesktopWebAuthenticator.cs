using Dauer.Model;
using Dauer.Ui.Desktop.Oidc;
using Dauer.Ui.Infra;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using Serilog;

namespace Dauer.Ui.Desktop;

public class DesktopWebAuthenticator : IWebAuthenticator
{
  private readonly string redirectUri_ = $"https://www.fitedit.io/login-redirect.html";
  private readonly string authority_ = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_nqQT8APwr";
  private readonly string api_ = "https://api.fitedit.io/";
  private readonly string clientId_ = "5n3lvp2jfo1c2kss375jvkhvod";

  private OidcClient? oidcClient_;
  private HttpClient? httpClient_;

  public DesktopWebAuthenticator()
  {
    _ = Task.Run(InitAsync);
  }

  private void InitAsync()
  {
    var browser = new DesktopBrowser();

    var options = new OidcClientOptions
    {
      Authority = authority_,
      ClientId = clientId_,
      RedirectUri = redirectUri_,
      Scope = "email openid",
      FilterClaims = false,
      Browser = browser,
      // JS on the redirect page will split the state on the .
      // to get the local listen port
      AdditionalState = $".port={browser.Port}"
    };
    options.Policy.Discovery.ValidateEndpoints = false;

    var serilog = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
        .CreateLogger();

    options.LoggerFactory.AddSerilog(serilog);

    oidcClient_ = new OidcClient(options);
    httpClient_ = new HttpClient { BaseAddress = new Uri(api_) };
  }

  public async Task AuthenticateAsync()
  {
    Dauer.Model.Log.Info($"{nameof(DesktopWebAuthenticator)}.{nameof(AuthenticateAsync)}");

    if (oidcClient_ == null) { return; }

    var result = await oidcClient_.LoginAsync();
    LogResult(result);
    await GetIsAuthenticated(result.AccessToken);
  }

  private static void LogResult(LoginResult result)
  {
    if (result.IsError)
    {
      Dauer.Model.Log.Error($"Auth error: {result.Error}");
      return;
    }

    Dauer.Model.Log.Info("Claims:");
    foreach (var claim in result.User.Claims)
    {
      Dauer.Model.Log.Info($"{claim.Type}: {claim.Value}");
    }

    Dauer.Model.Log.Info($"Identity token: {result.IdentityToken}");
    Dauer.Model.Log.Info($"Access token:   {result.AccessToken}");
    Dauer.Model.Log.Info($"Refresh token:  {result.RefreshToken ?? "none"}");
  }

  private async Task<bool> GetIsAuthenticated(string accessToken)
  {
    if (httpClient_ == null) { return false; }

    httpClient_.SetBearerToken(accessToken);
    var response = await httpClient_.GetAsync("auth");

    if (response.IsSuccessStatusCode)
    {
      Dauer.Model.Log.Info("Successfully authenticated");
      return true;
    }

    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
    {
      Dauer.Model.Log.Error($"Not authenticated: 401 Unauthorized");
      return false;
    }

    Dauer.Model.Log.Error($"Not authenticated: {response.ReasonPhrase}");
    return false;
  }
}