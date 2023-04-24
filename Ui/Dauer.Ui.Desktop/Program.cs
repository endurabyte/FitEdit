using System;
using System.Reflection;
using Avalonia;
using Avalonia.ReactiveUI;
using Dauer.Ui;

namespace Dauer.Ui.Desktop;

internal class Program
{
  // Initialization code. Don't use any Avalonia, third-party APIs or any
  // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
  // yet and stuff might break.
  [STAThread]
  public static void Main(string[] args)
  {
    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
    {
      string to = Assembly.GetAssembly(typeof(Fuse.Fuse))?.Location ?? "";
      return args.Name.StartsWith("Dauer")
        ? Fuse.Fuse.Redirect(args.Name, to)
        : null;
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
