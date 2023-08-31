using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;
using Avalonia.ReactiveUI;
using Dauer.Ui.Infra;

[assembly: SupportedOSPlatform("browser")]
namespace Dauer.Ui.Browser;

internal partial class Program
{
  private static async Task Main(string[] args)
  {
    App.Root = ConfigurationRoot.Bootstrap(new BrowserCompositionRoot());

    await BuildAvaloniaApp()
      .UseReactiveUI()
      .StartBrowserAppAsync("out");
  }

  public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>()
          .LogToTrace(LogEventLevel.Debug);
}