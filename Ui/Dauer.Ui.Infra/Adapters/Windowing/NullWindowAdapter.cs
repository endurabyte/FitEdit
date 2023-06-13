using Avalonia.Controls;

namespace Dauer.Ui.Infra.Adapters.Windowing;

public class NullWindowAdapter : WindowAdapter, IWindowAdapter
{
  public Control? Main => new();
}
