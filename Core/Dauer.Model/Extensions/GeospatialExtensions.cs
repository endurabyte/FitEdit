using System.Text.RegularExpressions;

namespace Dauer.Model.Extensions;

public static partial class GeospatialExtensions
{
  public static double ToDegrees(this int semicircles) => semicircles * 180.0 / Math.Pow(2, 31);
  public static int ToSemicirlces(this double degrees) => (int)(degrees * Math.Pow(2, 31) / 180.0);

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
