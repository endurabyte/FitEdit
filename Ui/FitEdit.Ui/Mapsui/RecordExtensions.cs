using NetTopologySuite.Geometries;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using FitEdit.Model.Extensions;

namespace FitEdit.Ui.Mapsui;

public static class RecordExtensions
{
  public static Coordinate MapCoordinate(this Dynastream.Fit.RecordMesg r) => SphericalMercator
    .FromLonLat(
      (r.GetPositionLong() ?? 0).ToDegrees().ClampLongitude(),
      (r.GetPositionLat() ?? 0).ToDegrees().ClampLatitude())
    .ToCoordinate();
}
