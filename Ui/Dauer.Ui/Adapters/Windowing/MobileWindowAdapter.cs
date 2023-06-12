using Avalonia.Controls;
using Avalonia.Threading;

namespace Dauer.Ui.Adapters.Windowing;

public class MobileWindowAdapter : WindowAdapter, IWindowAdapter
{
  public Control? Main => MobileAdapter.App?.MainView;

  static MobileWindowAdapter()
  {
    // On iOS, MainView must be accessed from the main thread
    // TODO we know when the MainView is available in CompositionRoot; no need to poll.
    _ = Dispatcher.UIThread?.InvokeAsync(async () =>
    {
      while (MobileAdapter.App?.MainView == null)
      {
        await Task.Delay(1000);
      }

      if (MobileAdapter.App.MainView.IsLoaded)
      {
        HandleMainViewLoaded(null, null!);
      }
      else
      {
        MobileAdapter.App.MainView.Loaded += HandleMainViewLoaded;
      }
    });
  }

  private static void HandleMainViewLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => MobileAdapter.App!.MainView!.SizeChanged += HandleMainViewSizeChanged;
  private static void HandleMainViewSizeChanged(object? sender, SizeChangedEventArgs e) => resized_.OnNext((e.NewSize.Width, e.NewSize.Height));
}