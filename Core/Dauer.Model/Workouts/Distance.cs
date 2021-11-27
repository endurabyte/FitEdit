using Dauer.Model.Units;

namespace Dauer.Model.Workouts
{
  public class Distance
  {
    public double Value { get; set; }
    public DistanceUnit Unit { get; set; }

    public double Meters() => Unit.Meters(Value);
    public double Miles() => Unit.Miles(Value);
  }
}