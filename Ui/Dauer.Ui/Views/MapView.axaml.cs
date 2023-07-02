using Avalonia.Controls;
using Avalonia.Input;
using Dauer.Ui.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using NetTopologySuite.Geometries;

namespace Dauer.Ui.Views;

public static class PointExtensions 
{
  public static global::Mapsui.MPoint MapMPoint(this Avalonia.Point p) => new(p.X, p.Y);
  public static Avalonia.Point MapAvaloniaPoint(this global::Mapsui.MPoint mp) => new(mp.X, mp.Y);
  public static Point MapNtsPoint(this Avalonia.Point p) => new(p.X, p.Y);
  public static Point MapNtsPoint(this Coordinate c) => new(c.X, c.Y);
  public static Coordinate MapCoordinate(this Avalonia.Point p) => new(p.X, p.Y);
}

public partial class MapView : UserControl
{
  private IMapViewModel? vm_;
  private Geometry? selectedFeature_;
  private bool isDragging_;

  public MapView()
  {
    InitializeComponent();

    DataContextChanged += HandleDataContextChanged;
    MapControl.PointerPressed += HandlePointerPressed;
    MapControl.PointerMoved += HandlePointerMoved;
    MapControl.PointerReleased += HandlePointerReleased;
  }

  private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
  {
    if (vm_ == null) { return; }

    var viewport = MapControl.Map.Navigator.Viewport;

    Avalonia.Point screenPosition = e.GetPosition(MapControl);
    Avalonia.Point worldPosition = viewport
      .ScreenToWorld(screenPosition.MapMPoint())
      .MapAvaloniaPoint();

    var layer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "GPS Trace");

    if (layer == null) { return; }
    if (layer is not MemoryLayer ml) { return; }

    var gf = ml.Features.FirstOrDefault(f => f is global::Mapsui.Nts.GeometryFeature) as global::Mapsui.Nts.GeometryFeature;
    if (gf == null) { return; }
    
    var gc = gf.Geometry as GeometryCollection;
    if (gc == null) { return; }

    var gcPoints = gc.Select(geom => geom as Point).ToArray();

    var world = worldPosition.MapNtsPoint();
    var dists = gcPoints.Select(pt => NetTopologySuite.Operation.Distance.DistanceOp.Distance(pt, world)).ToList();
    dists.Sort();
    var point = gcPoints.FirstOrDefault(pt => NetTopologySuite.Operation.Distance.DistanceOp.IsWithinDistance(pt, world, 10));

    if (point == null) { return; }

    selectedFeature_ = point;
    isDragging_ = true;
  }

  private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
  {
    if (vm_ == null) { return; }
    isDragging_ = false;
    selectedFeature_ = null;
  }

  private void HandlePointerMoved(object? sender, PointerEventArgs e)
  {
    if (vm_ == null) { return; }
    if (!isDragging_) { return; }

    var screenPosition = e.GetPosition(MapControl);
    var viewport = MapControl.Map.Navigator.Viewport;
    var worldPosition = viewport.ScreenToWorld(screenPosition.MapMPoint());

    //if (selectedFeature_ is not global::Mapsui.Nts.GeometryFeature gf || gf.Geometry is not Point point)
    //{
    //  return;
    //}

    if (selectedFeature_ is not Point point) { return; }

    point.X = worldPosition.X;
    point.Y = worldPosition.Y;

    MapControl.Refresh();
  }

  private void HandleDataContextChanged(object? sender, EventArgs e)
  {
    if (DataContext is not IMapViewModel vm)
    {
      return;
    }

    vm_ = vm;
    vm_.Map = MapControl;
  }
}