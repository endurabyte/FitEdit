using Dauer.Ui.Browser.Adapters.Windowing;
using Dauer.Ui.Infra;

namespace Dauer.Ui.Browser;

public class BrowserWebAuthenticator : IWebAuthenticator
{
  public Task AuthenticateAsync()
  {
    WebWindowAdapter.OpenWindow("login.html", "login");
    return Task.CompletedTask;
  }
}