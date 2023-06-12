using Dauer.Ui.Adapters.Windowing;

namespace Dauer.Ui.Browser;

public class BrowserWebAuthenticator : IWebAuthenticator
{
  public Task AuthenticateAsync()
  {
    WebWindowAdapter.OpenWindow("login.html", "login");
    return Task.CompletedTask;
  }
}