namespace Dauer.Model.Units
{
  public static class UnitExtensions
  {
    /// <summary>
    /// Return unit conversions to m/s
    /// </summary>
    public static Dictionary<SpeedUnit, double> MetersPerSecondConversions = new()
    {
      [SpeedUnit.KmPerHour] = 0.277778,
      [SpeedUnit.MetersPerSecond] = 1.0,
      [SpeedUnit.MinPerMi] = 26.8224,
      [SpeedUnit.MinPerKm] = 16.6666667,
      [SpeedUnit.MiPerHour] = 0.44704,
    };

    /// <summary>
    /// Return the multiplier to convert 
    /// the time component of the given unit to seconds
    /// </summary>
    public static Dictionary<SpeedUnit, double> SecondsConversions = new()
    {
      [SpeedUnit.KmPerHour] = 3600,
      [SpeedUnit.MetersPerSecond] = 1.0,
      [SpeedUnit.MinPerMi] = 0.01666666666, // 1/60s
      [SpeedUnit.MinPerKm] = 0.01666666666, // 1/60s
      [SpeedUnit.MiPerHour] = 3600,
    };

    /// <summary>
    /// Return unit conversions to meter
    /// </summary>
    public static Dictionary<DistanceUnit, double> DistanceMeterConversions = new()
    {
      [DistanceUnit.Kilometer] = 1e-3,
      [DistanceUnit.Meter] = 1,
      [DistanceUnit.Mile] = 1609.34,
    };

    /// <summary>
    /// Convert the given speed unit to per second
    /// </summary>
    public static double PerSecond(this SpeedUnit unit, double d) => d * SecondsConversions[unit];

    /// <summary>
    /// Convert the given speed unit to meters per second
    /// </summary>
    public static double MetersPerSecond(this SpeedUnit unit, double d) => d * MetersPerSecondConversions[unit];

    /// <summary>
    /// Convert the given speed unit to minutes per mile
    /// </summary>
    public static double MinutesPerMile(this SpeedUnit unit, double d) => d < 1e-12 ? 0 : MetersPerSecondConversions[SpeedUnit.MinPerMi] / (d * MetersPerSecondConversions[unit]);

    /// <summary>
    /// Convert the given distance unit to meters
    /// </summary>
    public static double Meters(this DistanceUnit unit, double d) => d * DistanceMeterConversions[unit];

    /// <summary>
    /// Convert the given distance unit to miles
    /// </summary>
    public static double Miles(this DistanceUnit unit, double d) => d * DistanceMeterConversions[unit] / DistanceMeterConversions[DistanceUnit.Mile];
  }
}
