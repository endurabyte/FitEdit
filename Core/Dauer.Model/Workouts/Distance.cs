using Dauer.Model.Units;

namespace Dauer.Model.Workouts
{
  public class Distance
  {
    public double Value { get; set; }
    public DistanceUnit Unit { get; set; }

    public Distance() { }
    public Distance(double value, DistanceUnit unit)
    {
      Value = value;
      Unit = unit;
    }

    public double Meters() => Unit.Meters(Value);
    public double Miles() => Unit.Miles(Value);

    public Distance Convert(DistanceUnit to) => new(Unit.Convert(to, Value), to);
  }
}