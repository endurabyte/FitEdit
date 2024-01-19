using FitEdit.Ui.Browser.Adapters.Windowing;
using FitEdit.Ui.Infra;

namespace FitEdit.Ui.Browser;

public class BrowserWebAuthenticator : WebAuthenticatorBase
{
  public override Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    WebWindowAdapter.OpenWindow("login.html", "login");
    return Task.FromResult(true);
  }
}