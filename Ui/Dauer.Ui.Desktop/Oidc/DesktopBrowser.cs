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

    Browser.Open(options.StartUrl);

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
}
