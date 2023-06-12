namespace Dauer.Ui.Extensions;

public static class GeospatialExtensions
{
  public static double ToDegrees(this int semicircles) => semicircles * 180.0 / Math.Pow(2, 31);
}
