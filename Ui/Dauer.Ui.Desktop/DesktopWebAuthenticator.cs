using System.Diagnostics;
using Dauer.Model;

namespace Dauer.Ui.Desktop;

public class DesktopWebAuthenticator : IWebAuthenticator
{
  public Task AuthenticateAsync()
  {
    Log.Info($"{nameof(DesktopWebAuthenticator)}.{nameof(AuthenticateAsync)}");

    var url = "https://auth.fitedit.io/login?response_type=code&client_id=667gbn7s1enf2jjnmnai6gc8o5&redirect_uri=https://app.fitedit.io";

    var psi = new ProcessStartInfo
    {
      FileName = url,
      UseShellExecute = true,
    };

    Process.Start(psi);
    return Task.CompletedTask;
  }
}
