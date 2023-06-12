using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Dauer.Ui.Adapters.Windowing;

public class DesktopWindowAdapter : WindowAdapter, IWindowAdapter
{
  public Control? Main => DesktopAdapter.App?.MainWindow;

  static DesktopWindowAdapter() => DesktopAdapter.App!.Startup += HandleAppStartup;

  private static void HandleAppStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e) => DesktopAdapter.App!.MainWindow!.SizeChanged += HandleWindowSizeChanged;

  private static void HandleWindowSizeChanged(object? sender, SizeChangedEventArgs e) => resized_.OnNext((e.NewSize.Width, e.NewSize.Height));
}
