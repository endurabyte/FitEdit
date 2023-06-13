using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters;
using Dauer.Ui.Infra.Adapters.Storage;
using Dauer.Ui.Infra.Adapters.Windowing;

[assembly: SupportedOSPlatform("browser")]
namespace Dauer.Ui.Browser;

internal partial class Program
{
  private static async Task Main(string[] args)
  {
    _ = JSHost
      .ImportAsync(WebStorageAdapterImpl.ModuleName, "./store.js")
      .ContinueWith(_ =>
      {
        WebConsoleAdapter.Log($"{WebStorageAdapterImpl.ModuleName} ready");
        string key = "testKey";
        WebStorageAdapterImpl.SetLocalStorage(key, "{ \"jsonKey\" : \"jsonValue\" }");
        string data = WebStorageAdapterImpl.GetLocalStorage(key);
        WebConsoleAdapter.Log($"Got from storage: {key} => {data}");
      });

    _ = JSHost
      .ImportAsync(WebConsoleAdapter.ModuleName, "./console.js")
      .ContinueWith(_ =>
      {
        WebConsoleAdapter.Log($"{WebConsoleAdapter.ModuleName} ready");
        WebConsoleAdapter.SetMessage();
      });

    _ = JSHost
      .ImportAsync(WebWindowAdapterImpl.ModuleName, "./windowing.js")
      .ContinueWith(_ =>
      {
        WebConsoleAdapter.Log($"{WebWindowAdapterImpl.ModuleName} ready");
        WebWindowAdapterImpl.ListenForResize();
        WebWindowAdapterImpl.ListenForMessages();
      });

    CompositionRoot.ServiceLocator.Register<IWebAuthenticator>(new BrowserWebAuthenticator());
    
    await BuildAvaloniaApp()
      .UseReactiveUI()
      .StartBrowserAppAsync("out");
  }

  public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .LogToTrace(LogEventLevel.Debug);
}