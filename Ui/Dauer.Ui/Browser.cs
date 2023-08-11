using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dauer.Ui;

public class Browser
{
  public static void Open(string? url)
  {
    if (url == null) { return; }

    try
    {
      Process.Start(url);
    }
    catch
    {
      // hack because of this: https://github.com/dotnet/corefx/issues/10361
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        url = url.Replace("&", "^&");
        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        Process.Start("xdg-open", url);
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        Process.Start("open", url);
      }
      else
      {
        throw;
      }
    }
  }

}
