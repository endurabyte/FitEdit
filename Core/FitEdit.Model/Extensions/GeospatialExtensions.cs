using System.Text.RegularExpressions;

namespace FitEdit.Model.Extensions;

public static partial class GeospatialExtensions
{
  public static double DegreeToSemicircles = 11930464.71111111;
  public static double SemicirclesToDegree = 8.381903171539307e-08;
    
  public static double ToDegrees(this int semicircles) => semicircles * 180.0 / Math.Pow(2, 31);
  public static int ToSemicircles(this double degrees) => (int)(degrees * Math.Pow(2, 31) / 180.0);

  /// <summary>
  /// Latitude must be between -90 and 90.
  /// We clamp it further because close to the extremes the projection is not accurate.
  /// </summary>
  public static double ClampLatitude(this double degrees) => Math.Max(-80, Math.Min(80, degrees));
  /// <summary>
  /// Longitude must be between -180 and 180
  /// </summary>
  public static double ClampLongitude(this double degrees) => Math.Max(-180, Math.Min(180, degrees));

  /// <summary>
  /// Extract e.g. -34.1515926 from the strings "-34.1515926 °W" or "-34.1515926°N"
  /// </summary>
  public static bool TryGetCoordinate(string coordinate, out double result)
  {
    Match match = regex().Match(coordinate);

    if (match.Success)
    {
      return double.TryParse(match.Value, out result);
    }

    result = 0;
    return false;
  }

  [GeneratedRegex("-?\\d+(\\.\\d+)?", RegexOptions.Compiled)]
  private static partial Regex regex();
}
