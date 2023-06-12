using Avalonia.Controls;

namespace Dauer.Ui.Adapters.Windowing;

public class NullWindowAdapter : WindowAdapter, IWindowAdapter
{
  public Control? Main => new();
}
