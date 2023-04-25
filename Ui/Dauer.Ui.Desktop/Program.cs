using System;
using System.Reflection;
using Avalonia;
using Avalonia.ReactiveUI;
using Dauer.Fuse.Secure;

namespace Dauer.Ui.Desktop;

internal class Program
{
  /// <summary>
  /// Full path to Dauer.Fuse.dll
  /// </summary>
  private static readonly string fuse_ = Assembly.GetAssembly(typeof(Defuse))?.Location?.Replace(".Secure", "") ?? "";

  // Initialization code. Don't use any Avalonia, third-party APIs or any
  // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
  // yet and stuff might break.
  [STAThread]
  public static void Main(string[] args)
  {
    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => args.Name.StartsWith("Dauer")
      ? Defuse.Redirect(args.Name, fuse_)
      : null;

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
