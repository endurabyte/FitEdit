using Mapsui;
using NetTopologySuite.Geometries;

namespace FitEdit.Ui.Extensions;

public static class PointExtensions
{
  public static MPoint MapMPoint(this Avalonia.Point p) => new(p.X, p.Y);
  public static Avalonia.Point MapAvaloniaPoint(this MPoint mp) => new(mp.X, mp.Y);
  public static Coordinate MapCoordinate(this MPoint mp) => new(mp.X, mp.Y);
  public static Point MapNtsPoint(this Avalonia.Point p) => new(p.X, p.Y);
  public static Point MapNtsPoint(this Coordinate c) => new(c.X, c.Y);
  public static MPoint MapMPoint(this Coordinate c) => new(c.X, c.Y);
  public static Coordinate MapCoordinate(this Avalonia.Point p) => new(p.X, p.Y);
}
