using Dauer.Model.Workouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Model;

public class Lap : ReactiveObject
{
  [Reactive] public DateTime Start { get; set; }
  [Reactive] public DateTime End { get; set; }
  [Reactive] public Speed? Speed { get; set; }
  [Reactive] public Distance? Distance { get; set; }

  public TimeSpan Duration => End - Start;
  public double DistanceMiles => Distance?.Miles() ?? 0;
}
