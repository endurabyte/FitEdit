using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Units;

namespace FitEdit.Model.Workouts
{
  public class Distance : ReactiveObject
  {
    [Reactive] public double Value { get; set; }
    [Reactive] public Unit Unit { get; set; }

    public Distance() { }

    public Distance(string s)
    {
      if (string.IsNullOrEmpty(s))
      {
        return;
      }

      var q = new ParsedQuantity(s);
      Value = q.Value;
      Unit = UnitMapper.MapUnit(q.Unit);
    }

    public Distance(Distance other)
    {
      Value = other.Value;
      Unit = other.Unit;
    }

    public Distance(double value, Unit unit)
    {
      Value = value;
      Unit = unit;
    }

    public double Meters() => UnitConvert.Convert(Unit, Unit.Meter, Value);
    public double Miles() => UnitConvert.Convert(Unit, Unit.Mile, Value);

    public Distance Convert(Unit to) => new(UnitConvert.Convert(Unit, to, Value), to);

    public override string ToString() => $"{Value:0.##}{Unit.MapString()}";
  }
}