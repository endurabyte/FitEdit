using Dauer.Ui.Browser.Adapters.Windowing;
using Dauer.Ui.Infra;

namespace Dauer.Ui.Browser;

public class BrowserWebAuthenticator : WebAuthenticatorBase
{
  public override Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    WebWindowAdapter.OpenWindow("login.html", "login");
    return Task.FromResult(true);
  }
}