using Dauer.Model.Workouts;
using ReactiveUI;

namespace Dauer.Ui.Models;

public class Lap : ReactiveObject
{
  private DateTime start_;
  private DateTime end_;
  private Speed? speed_;

  public DateTime Start { get => start_; set => this.RaiseAndSetIfChanged(ref start_, value); }
  public DateTime End { get => end_; set => this.RaiseAndSetIfChanged(ref end_, value); }
  public Speed? Speed { get => speed_; set => this.RaiseAndSetIfChanged(ref speed_, value); }
  
  public double DurationSeconds => (End - Start).TotalSeconds;
}
