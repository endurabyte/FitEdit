using Avalonia.Controls;

namespace FitEdit.Ui.Model;

public interface IWindowAdapter
{
  /// <summary>
  /// (width, height)
  /// </summary>
  IObservable<(double, double)> Resized { get; }
  Control? Main { get; }
}
