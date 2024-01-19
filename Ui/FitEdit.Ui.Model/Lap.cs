#nullable enable
using FitEdit.Model.Workouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.Model;

public class Lap : ReactiveObject
{
  [Reactive] public DateTime Start { get; set; }
  [Reactive] public DateTime End { get; set; }
  [Reactive] public Speed Speed { get; set; } = new() { Unit = Units.Unit.MetersPerSecond };
  [Reactive] public Distance Distance { get; set; } = new() { Unit = Units.Unit.Meter };

  /// <summary>
  /// Index of first record for this lap.
  /// </summary>
  public int RecordFirstIndex { get; set; }

  /// <summary>
  /// Index of the last record for this lap
  /// </summary>
  public int RecordLastIndex { get; set; }

  public TimeSpan Duration => End - Start;

  public Lap() { }

  public Lap(Lap other)
  {
    Start = other.Start;
    End = other.End;
    Speed = new Speed(other.Speed);
    Distance = new Distance(other.Distance);
    RecordFirstIndex = other.RecordFirstIndex;
    RecordLastIndex = other.RecordLastIndex;
  }
}
