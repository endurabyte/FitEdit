using Avalonia.Controls;

namespace Dauer.Ui.Adapters.Windowing;

public class WebWindowAdapter : WindowAdapter, IWindowAdapter
{
  private static readonly WebControl main_ = new();

  public Control? Main => main_;

  static WebWindowAdapter()
  {
    if (!OperatingSystem.IsBrowser())
    {
      return;
    }

    WebWindowAdapterImpl.Resized.Subscribe(tuple =>
    {
      resized_.OnNext(tuple);
      main_.SetBounds(tuple.Item1, tuple.Item2);
    });
  }
}
