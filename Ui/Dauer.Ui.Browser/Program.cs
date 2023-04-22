using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Dauer.Ui;
using Dauer.Ui.Services;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
  private static void Main(string[] args)
  {
    _ = JSHost
      .ImportAsync(WebStorage.ModuleName, "./main.js")
      .ContinueWith(_ =>
      {
        WebConsole.Log($"{WebStorage.ModuleName} ready");
        string key = "testKey";
        WebStorage.SetLocalStorage(key, "{ \"jsonKey\" : \"jsonValue\" }");
        string data = WebStorage.GetLocalStorage(key);
        WebConsole.Log($"Got from storage: {key} => {data}");
        WebStorage.SetMessage();
      });

    _ = JSHost
      .ImportAsync(WebConsole.ModuleName, "./main.js")
      .ContinueWith(_ =>
      {
        WebConsole.Log($"{WebConsole.ModuleName} ready");
      });

    BuildAvaloniaApp()
      .UseReactiveUI()
      .SetupBrowserApp("out");
  }

  public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .LogToTrace(LogEventLevel.Debug);
}