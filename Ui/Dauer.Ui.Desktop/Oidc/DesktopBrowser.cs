using System.Diagnostics;
using System.Runtime.InteropServices;
using IdentityModel.OidcClient.Browser;

namespace Dauer.Ui.Desktop.Oidc;

public class DesktopBrowser : IBrowser
{
  public int Port { get; }

  public DesktopBrowser(int? port = null)
  {
    Port = port == null 
      ? Tcp.GetRandomUnusedPort() 
      : port.Value;
  }

  public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken ct = default)
  {
    var content = await new LoginRedirectContent().LoadContentAsync(ct);
    using var listener = new LoopbackHttpListener(content.SuccessHtml, content.ErrorHtml, Port);

    OpenBrowser(options.StartUrl);

    if (options.Timeout == TimeSpan.Zero)
    {
      return new BrowserResult { Response = "Logout success", ResultType = BrowserResultType.Success };
    }

    try
    {
      string? result = await listener.WaitForCallbackAsync(ct);
      return string.IsNullOrWhiteSpace(result)
        ? new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = "Empty response" }
        : new BrowserResult { Response = result, ResultType = BrowserResultType.Success };
    }
    catch (TaskCanceledException ex)
    {
      return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = ex.Message };
    }
    catch (Exception ex)
    {
      return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
    }
  }

  public static void OpenBrowser(string? url)
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
