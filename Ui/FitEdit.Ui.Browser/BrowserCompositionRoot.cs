using System.Runtime.InteropServices.JavaScript;
using Autofac;
using FitEdit.Model;
using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using FitEdit.Ui.Browser.Adapters;
using FitEdit.Ui.Browser.Adapters.Storage;
using FitEdit.Ui.Browser.Adapters.Windowing;
using FitEdit.Ui.Infra;
using FitEdit.Ui.Model;

namespace FitEdit.Ui.Browser;

public class BrowserCompositionRoot : CompositionRoot
{
  static BrowserCompositionRoot()
  {
    // Not necessary; Console.WriteLine already writes to web browser console
    //if (OperatingSystem.IsBrowser())
    //{
    //  Log.Sinks.Add(Adapters.WebConsoleAdapter.Log);
    //}
  }

  protected override async Task ConfigureAsync(ContainerBuilder builder)
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
        Log.Info($"{WebWindowAdapterImpl.ModuleName} ready");
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

    builder.RegisterType<BrowserWebAuthenticator>().As<IWebAuthenticator>();
    builder.RegisterType<WebWindowAdapter>().As<IWindowAdapter>();
    builder.RegisterType<WebStorageAdapter>().As<IWindowAdapter>();
    builder.RegisterType<IdbfsSqliteAdapter>().As<IDatabaseAdapter>()
      .WithParameter("dbPath", $"{dir}{Path.DirectorySeparatorChar}{db}")
      .SingleInstance();

    await base.ConfigureAsync(builder);
  }

  private static async Task InitDb(string db, string dir)
  {
    await WebStorageAdapterImpl.MountAndInitializeDb();
    string origin = WebWindowAdapterImpl.GetOrigin();

    var client = new HttpClient
    {
      BaseAddress = new Uri(origin)
    };

    string dest = $"{dir}{Path.DirectorySeparatorChar}{db}";

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
}