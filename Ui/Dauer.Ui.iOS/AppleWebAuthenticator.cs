using Dauer.Model;
using Microsoft.Maui.Authentication;

namespace Dauer.Ui.iOS;

public class AppleWebAuthenticator : IWebAuthenticator
{
  public async Task AuthenticateAsync()
  {
    Log.Info($"{nameof(AppleWebAuthenticator)}.{nameof(AuthenticateAsync)}");

    // Make sure to enable Apple Sign In in both the
    // entitlements and the provisioning profile.
    var options = new AppleSignInAuthenticator.Options
    {
      IncludeEmailScope = true,
      IncludeFullNameScope = true,
    };

    WebAuthenticatorResult? result = await AppleSignInAuthenticator.AuthenticateAsync(options);
  }
}
