using Dauer.Model;
using Microsoft.Maui.Authentication;

namespace Dauer.Ui.iOS;

public class AppleWebAuthenticator : Infra.IWebAuthenticator
{
  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
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
    return result?.AccessToken != null;
  }
}
