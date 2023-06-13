using NetTopologySuite.Geometries;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Dauer.Ui.Extensions;

namespace Dauer.Ui.Mapsui;

public static class RecordExtensions
{
  public static Coordinate MapCoordinate(this Dynastream.Fit.RecordMesg r) => SphericalMercator
    .FromLonLat(
      (r.GetPositionLong() ?? 0).ToDegrees(),
      (r.GetPositionLat() ?? 0).ToDegrees())
    .ToCoordinate();
}
