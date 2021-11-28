using Dauer.Model.Units;

namespace Dauer.Model.Workouts
{
  public class Speed
  {
    public double Value { get; set; }
    public SpeedUnit Unit { get; set; }

    public Speed() { }

    public Speed(double value, SpeedUnit unit)
    {
      Value = value;
      Unit = unit;
    }

    public Speed(double value, string unit) : this(value, SpeedUnitMapper.Map(unit))
    {
      
    }

    public double MetersPerSecond() => Unit.MetersPerSecond(Value);
    public double MinutesPerMile() => Unit.MinutesPerMile(Value);
  }
}