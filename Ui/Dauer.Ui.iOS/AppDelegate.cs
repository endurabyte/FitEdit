using Foundation;
using UIKit;
using Avalonia;
using Avalonia.iOS;
using Avalonia.ReactiveUI;
using Microsoft.Maui.ApplicationModel;
using Dauer.Ui.Infra;

namespace Dauer.Ui.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
  protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
  {
    App.Root = ConfigurationRoot.Bootstrap(new AppleCompositionRoot());

    return base.CustomizeAppBuilder(builder)
      .AfterSetup(_ => Platform.Init(() => Window.RootViewController!))
      .UseReactiveUI();
  }
}