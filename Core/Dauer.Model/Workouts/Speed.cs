using Units;

namespace Dauer.Model.Workouts
{
  public class Speed : ModelBase
  {
    private double value_;
    private Unit unit_;

    public double Value { get => value_; set => Set(ref value_, value); }
    public Unit Unit { get => unit_; set => Set(ref unit_, value); }

    public Speed() { }

    public Speed(double value, Unit unit)
    {
      Value = value;
      Unit = unit;
    }

    public Speed(string s)
    {
      if (s.Contains(':'))
      {
        var time = new ParsedTime(s);
        Value = time.Value;
        Unit = UnitMapper.MapUnit(time.Unit);
        return;
      }

      var q = new ParsedQuantity(s);
      Value = q.Value;
      Unit = UnitMapper.MapUnit(q.Unit);
    }

    public Speed(double value, string unit) : this(value, UnitMapper.MapUnit(unit))
    {
      
    }

    public Speed Convert(Unit to) => new(UnitConvert.Convert(Unit, to, Value), to);

    public static bool operator ==(Speed lhs, Speed rhs) => lhs?.Equals(rhs) ?? false;
    public static bool operator !=(Speed lhs, Speed rhs) => !(lhs?.Equals(rhs) ?? false);

    public override bool Equals(object obj) => obj is Speed s && s.Convert(Unit.MetersPerSecond).Value == Convert(Unit.MetersPerSecond).Value;

    public override int GetHashCode() => HashCode.Combine(Value, Unit);

    public override string ToString() => Unit switch
    {
      Unit.MinutesPerMile => $"{MinuteString(Value)}{Unit.MapString()}",
      Unit.MinutesPerKilometer => $"{MinuteString(Value)}{Unit.MapString()}",
      _ => $"{Value:0.##}{Unit.MapString()}",
    };

    // Print the fractional part of the given number as
    // seconds of a minute e.g. 8.9557 => 8:57
    private static string MinuteString(double minutesPerX)
    {
      if (minutesPerX == double.PositiveInfinity || minutesPerX == double.NegativeInfinity)
      {
        return "0:00";
      }

      int floor = (int)Math.Floor(minutesPerX);
      return $"{floor}:{(int)((minutesPerX - floor)*60):00}";
    }
  }
}