using Avalonia.Controls;

namespace Dauer.Ui.Infra.Adapters.Windowing;

public interface IWindowAdapter
{
  IObservable<(double, double)> Resized { get; }
  Control? Main { get; }
}
