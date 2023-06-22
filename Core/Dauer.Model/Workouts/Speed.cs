using Dauer.Model.Units;

namespace Dauer.Model.Workouts
{
  public class Speed : ModelBase
  {
    private double value_;
    private SpeedUnit unit_;

    public double Value { get => value_; set => Set(ref value_, value); }
    public SpeedUnit Unit { get => unit_; set => Set(ref unit_, value); }

    public Speed() { }

    public Speed(double value, SpeedUnit unit)
    {
      Value = value;
      Unit = unit;
    }

    public Speed(double value, string unit) : this(value, SpeedUnitMapper.Map(unit))
    {
      
    }

    public Speed Convert(SpeedUnit to) => new(Unit.Convert(to, Value), to);

    public double MetersPerSecond() => Unit.MetersPerSecond(Value);
    public double MinutesPerMile() => Unit.MinutesPerMile(Value);

    public static bool operator ==(Speed lhs, Speed rhs) => lhs?.Equals(rhs) ?? false;
    public static bool operator !=(Speed lhs, Speed rhs) => !(lhs?.Equals(rhs) ?? false);

    public override bool Equals(object obj) => obj is Speed s && s.MetersPerSecond() == MetersPerSecond();

    public override int GetHashCode() => HashCode.Combine(Value, Unit);
  }
}