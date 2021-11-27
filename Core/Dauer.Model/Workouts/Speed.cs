using Dauer.Model.Units;

namespace Dauer.Model.Workouts
{
  public class Speed
  {
    public double Value { get; set; }
    public SpeedUnit Unit { get; set; }

    public double MetersPerSecond() => Unit.MetersPerSecond(Value);
    public double MinutesPerMile() => Unit.MinutesPerMile(Value);
  }
}