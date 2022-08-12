namespace Dauer.Model.Units
{
  public class SpeedUnitMapper
  {
    public static SpeedUnit Default { get; set; } = SpeedUnit.MiPerHour;

    public static SpeedUnit Map(string unit) => unit switch
    {
      "km/h" => SpeedUnit.KmPerHour,
      "m/s" => SpeedUnit.MetersPerSecond,
      "min/mi" => SpeedUnit.MinPerMi,
      "min/km" => SpeedUnit.MinPerKm,
      "mi/h" => SpeedUnit.MiPerHour,
      _ => Default,
    };
  }
}
