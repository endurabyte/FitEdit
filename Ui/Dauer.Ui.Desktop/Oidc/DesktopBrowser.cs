using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Dauer.Model;
using IdentityModel.OidcClient.Browser;

namespace Dauer.Ui.Desktop.Oidc;

public class DesktopBrowser : IBrowser
{
  public int Port { get; }
  private readonly string _path;

  private readonly string successDefaultHtml_ = "<h1>Login success. You can close this window</h1>";
  private readonly string successHtmlFile_ = "https://www.fitedit.io/login-success.html";
  private string successHtml_;

  private readonly string errorDefaultHtml_ = "<h1>There was an error.</h1>";
  private readonly string errorHtmlFile_ = "https://www.fitedit.io/login-error.html";
  private string errorHtml_;

  public DesktopBrowser(int? port = null, string? path = null)
  {
    successHtml_ = successDefaultHtml_;
    errorHtml_ = errorDefaultHtml_;

    _path = path ?? string.Empty;

    Port = port == null 
      ? GetRandomUnusedPort() 
      : port.Value;

    _ = Task.Run(LoadContentAsync);
  }

  private async Task LoadContentAsync()
  {
    var client = new HttpClient();

    try
    {
      successHtml_ = await client.GetStringAsync(successHtmlFile_);
      errorHtml_ = await client.GetStringAsync(errorHtmlFile_);
    }
    catch (Exception e)
    {
      Log.Error(e);
    }
  }

  private static int GetRandomUnusedPort()
  {
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
  }

  public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
  {
    using var listener = new LoopbackHttpListener(successHtml_, errorHtml_, Port, _path);

    OpenBrowser(options.StartUrl);

    if (options.Timeout == TimeSpan.Zero)
    {
      return new BrowserResult { Response = "Logout success", ResultType = BrowserResultType.Success };
    }

    try
    {
      string? result = await listener.WaitForCallbackAsync();
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

  public static void OpenBrowser(string url)
  {
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
