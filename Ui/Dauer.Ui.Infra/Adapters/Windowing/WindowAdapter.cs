using System.Reactive.Subjects;

namespace Dauer.Ui.Infra.Adapters.Windowing;

public class WindowAdapter
{
  public IObservable<(double, double)> Resized => resized_;
  protected static readonly ISubject<(double, double)> resized_ = new Subject<(double, double)>();
}
