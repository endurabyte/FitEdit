using Avalonia.Controls;

namespace Dauer.Ui.Infra.Adapters.Windowing;

public interface IWindowAdapter
{
  /// <summary>
  /// (width, height)
  /// </summary>
  IObservable<(double, double)> Resized { get; }
  Control? Main { get; }
}
