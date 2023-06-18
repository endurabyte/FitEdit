using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using Dauer.Model.Data;
using Dauer.Ui.Browser.Adapters;
using Dauer.Ui.Browser.Adapters.Storage;
using Dauer.Ui.Browser.Adapters.Windowing;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Storage;
using Dauer.Ui.Infra.Adapters.Windowing;

[assembly: SupportedOSPlatform("browser")]
namespace Dauer.Ui.Browser;

internal partial class Program
{
  private static async Task Main(string[] args)
  {
    await JSHost.ImportAsync(WebStorageAdapterImpl.ModuleName, "./store.js");

    WebConsoleAdapter.Log($"{WebStorageAdapterImpl.ModuleName} ready");
    string key = "testKey";
    WebStorageAdapterImpl.SetLocalStorage(key, "{ \"jsonKey\" : \"jsonValue\" }");
    string data = WebStorageAdapterImpl.GetLocalStorage(key);
    WebConsoleAdapter.Log($"Got from storage: {key} => {data}");

    await WebStorageAdapterImpl.MountAndInitializeDb();

    string db = "/database/fitedit.sqlite3";
    if (!File.Exists(db))
    {
      Dauer.Model.Log.Info($"Creating file {db}");
      File.Create(db).Close();
    }

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
    CompositionRoot.ServiceLocator.Register<IWindowAdapter>(new WebWindowAdapter());
    CompositionRoot.ServiceLocator.Register<IStorageAdapter>(new WebStorageAdapter());

    // More info on browser persistence: https://sqlite.org/wasm/doc/tip/persistence.md

    // Possible filenames:

    // Key-Value VFS
    // "local" for localStorage. "session" for sessionStorage.
    //true => "file:local?vfs=kvvfs", 

    // Origin-Private FileSystem
    //true => "file:fitedit.sqlite3?vfs=opfs", 

    // Manual sync of IndexedDB File System (IDBFS) via JavaScript. Each sync saves the whole in-memory to to disk.
    // https://www.thinktecture.com/blazor/ef-core-and-sqlite-in-browser/
    CompositionRoot.ServiceLocator.Register<IDatabaseAdapter>(new WasmSqliteAdapter("/database/fitedit.sqlite3"));

    await BuildAvaloniaApp()
      .UseReactiveUI()
      .StartBrowserAppAsync("out");
  }

  public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .LogToTrace(LogEventLevel.Debug);
}