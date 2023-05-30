using Dauer.Model;
using Microsoft.Maui.Authentication;

namespace Dauer.Ui.Android;

public class AndroidWebAuthenticator : IWebAuthenticator
{
  private const string authenticationUrl_ = "https://xamarin-essentials-auth-sample.azurewebsites.net/mobileauth/";
  public async Task AuthenticateAsync()
  {
    Log.Info($"{nameof(AndroidWebAuthenticator)}.{nameof(AuthenticateAsync)}");

    string scheme = "Google"; // try Microsoft, Google, Facebook, Apple

    var authUrl = new Uri(authenticationUrl_ + scheme);
    var callbackUrl = new Uri($"{WebAuthenticatorCallbackActivity.CallbackScheme}://");

    WebAuthenticatorResult r = await WebAuthenticator.AuthenticateAsync(authUrl, callbackUrl);
  }
}