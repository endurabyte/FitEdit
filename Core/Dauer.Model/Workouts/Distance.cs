using Units;

namespace Dauer.Model.Workouts
{
  public class Distance
  {
    public double Value { get; set; }
    public Unit Unit { get; set; }

    public Distance() { }

    public Distance(Distance other)
    {
      Value =other.Value;
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