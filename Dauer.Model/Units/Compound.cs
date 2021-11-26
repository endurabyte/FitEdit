using System.Collections.Generic;

namespace Dauer.Model.Units
{
  public static class Compound
  {
    /// <summary>
    /// Return unit conversions to m/s
    /// </summary>
    public static Dictionary<CompoundUnit, double> MetersPerSecondConversions = new()
    {
      [CompoundUnit.KmPerHour] = 0.277778,
      [CompoundUnit.MetersPerSecond] = 1.0,
      [CompoundUnit.MinPerMi] = 26.8224,
      [CompoundUnit.MinPerKm] = 16.6666667,
      [CompoundUnit.MiPerHour] = 44704,
    };

    /// <summary>
    /// Return the multiplier to convert 
    /// the time component of the given unit to seconds
    /// </summary>
    public static Dictionary<CompoundUnit, double> SecondsConversions = new()
    {
      [CompoundUnit.KmPerHour] = 3600,
      [CompoundUnit.MetersPerSecond] = 1.0,
      [CompoundUnit.MinPerMi] = 0.01666666666,
      [CompoundUnit.MinPerKm] = 0.01666666666,
      [CompoundUnit.MiPerHour] = 3600,
    };

    /// <summary>
    /// Convert the given compound unit to per seconds
    /// </summary>
    public static double OverSeconds(this CompoundUnit unit, double d) => d * SecondsConversions[unit];

    /// <summary>
    /// Convert the given compound unit to meters per second
    /// </summary>
    public static double ToMetersPerSecond(this CompoundUnit unit, double d) => d * MetersPerSecondConversions[unit];
  }
}
