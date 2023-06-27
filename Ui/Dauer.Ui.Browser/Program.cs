using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Security;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Model.Factories;
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
        Dauer.Model.Log.Info($"{WebWindowAdapterImpl.ModuleName} ready");
        WebWindowAdapterImpl.ListenForResize();
        WebWindowAdapterImpl.ListenForMessages();
      });

    await JSHost.ImportAsync(WebStorageAdapterImpl.ModuleName, "./store.js");

    WebConsoleAdapter.Log($"{WebStorageAdapterImpl.ModuleName} ready");
    string key = "testKey";
    WebStorageAdapterImpl.SetLocalStorage(key, "{ \"jsonKey\" : \"jsonValue\" }");
    string data = WebStorageAdapterImpl.GetLocalStorage(key);
    WebConsoleAdapter.Log($"Got from storage: {key} => {data}");

    string db = "fitedit.sqlite3";
    string dir = "/database";
    await InitDb(db, dir).AnyContext();

    CompositionRoot.ServiceLocator.Register<IWebAuthenticator>(new BrowserWebAuthenticator());
    CompositionRoot.ServiceLocator.Register<IWindowAdapter>(new WebWindowAdapter());
    CompositionRoot.ServiceLocator.Register<IStorageAdapter>(new WebStorageAdapter());

    CompositionRoot.ServiceLocator.Register<IDatabaseAdapter>(new IdbfsSqliteAdapter($"{dir}{Path.PathSeparator}{db}"));

    await BuildAvaloniaApp()
      .UseReactiveUI()
      .StartBrowserAppAsync("out");
  }

  private static async Task InitDb(string db, string dir)
  {
    await WebStorageAdapterImpl.MountAndInitializeDb();
    string origin = WebWindowAdapterImpl.GetOrigin();

    var client = new HttpClient
    {
      BaseAddress = new Uri(origin)
    };

    string dest = $"{dir}{Path.PathSeparator}{db}";

    if (File.Exists(dest))
    {
      return;
    }

    try
    {
      Task<byte[]> task1 = client.GetByteArrayAsync(db);
      Task<byte[]> task2 = client.GetByteArrayAsync($"{db}-shm");
      Task<byte[]> task3 = client.GetByteArrayAsync($"{db}-wal");

      await Task.WhenAll(task1, task2, task3);

      byte[]? dbFile = task1.Result;
      byte[]? shm = task2.Result;
      byte[]? wal = task3.Result;

      Task task4 = File.WriteAllBytesAsync($"{dest}", dbFile);
      Task task5 = File.WriteAllBytesAsync($"{dest}-shm", shm);
      Task task6 = File.WriteAllBytesAsync($"{dest}-wal", wal);

      await Task.WhenAll(task4, task5, task6);
    }
    catch (Exception e)
    {
      Log.Error(e);
    }
  }

  public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .LogToTrace(LogEventLevel.Debug);
}