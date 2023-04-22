﻿using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Dauer.Ui;
using Dauer.Ui.Adapters;
using Dauer.Ui.Adapters.Storage;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
  private static void Main(string[] args)
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

    BuildAvaloniaApp()
      .UseReactiveUI()
      .SetupBrowserApp("out");
  }

  public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .LogToTrace(LogEventLevel.Debug);
}