﻿using Avalonia;
using Avalonia.ReactiveUI;
using FitEdit.Ui.Infra;

namespace FitEdit.Ui.Desktop;

internal class Program
{
  // Initialization code. Don't use any Avalonia, third-party APIs or any
  // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
  // yet and stuff might break.
  [STAThread]
  public static void Main(string[] args)
  {
    App.Root = ConfigurationRoot.Bootstrap(new CompositionRoot());
    App.DidStart += root =>
    {
      var notifier = root.Get<INotifyService>();
      new AutoUpdater(notifier).WatchForUpdates();
    };

    BuildAvaloniaApp()
      .StartWithClassicDesktopLifetime(args);
  }

  // Avalonia configuration, don't remove; also used by visual designer.
  public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}