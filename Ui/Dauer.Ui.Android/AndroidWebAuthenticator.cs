using Dauer.Model;
using Microsoft.Maui.Authentication;

namespace Dauer.Ui.Android;

public class AndroidWebAuthenticator : Infra.WebAuthenticatorBase
{
  private const string authenticationUrl_ = "https://auth2.fitedit.io/login?response_type=code&client_id=667gbn7s1enf2jjnmnai6gc8o5&redirect_uri=fitedit://app.fitedit.io";

  public override async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    Log.Info($"{nameof(AndroidWebAuthenticator)}.{nameof(AuthenticateAsync)}");

    string scheme = "Google"; // try Microsoft, Google, Facebook, Apple

    var authUrl = new Uri(authenticationUrl_ + scheme);
    var callbackUrl = new Uri($"{WebAuthenticatorCallbackActivity.CallbackScheme}://");

    WebAuthenticatorResult r = await WebAuthenticator.AuthenticateAsync(authUrl, callbackUrl);
    return r.AccessToken != null;
  }
}