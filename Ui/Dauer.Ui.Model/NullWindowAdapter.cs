using System.Reactive.Subjects;
using Avalonia.Controls;

namespace Dauer.Ui.Model;

public class NullWindowAdapter : IWindowAdapter
{
  public IObservable<(double, double)> Resized { get; } = new Subject<(double, double)>();
  public Control? Main => new();
}
