using Avalonia.Controls;
using Avalonia.Input;
using FitEdit.Ui.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Providers;
using NetTopologySuite.Geometries;

namespace FitEdit.Ui.Views;

public static class PointExtensions 
{
  public static MPoint MapMPoint(this Avalonia.Point p) => new(p.X, p.Y);
  public static Avalonia.Point MapAvaloniaPoint(this MPoint mp) => new(mp.X, mp.Y);
  public static Point MapNtsPoint(this Avalonia.Point p) => new(p.X, p.Y);
  public static Point MapNtsPoint(this Coordinate c) => new(c.X, c.Y);
  public static MPoint MapMPoint(this Coordinate c) => new(c.X, c.Y);
  public static Coordinate MapCoordinate(this Avalonia.Point p) => new(p.X, p.Y);
}

public partial class MapView : UserControl
{
  private IMapViewModel? vm_;
  private PointFeature? draggedPoint_;
  private bool IsDragging_ => draggedPoint_ != null;
  private Viewport Viewport_ => MapControl.Map.Navigator.Viewport;

  public MapView()
  {
    InitializeComponent();

    DataContextChanged += HandleDataContextChanged;
    MapControl.PointerMoved += HandlePointerMoved;

    // We have two options to handle GPS trackpoint drag
    // 1. Manually handle pointer press/release. On press, find the GPS trackpoint under the pointer.
    //MapControl.PointerPressed += HandlePointerPressed;
    //MapControl.PointerReleased += HandlePointerReleased;

    // 2. Use the MapControl HandleInfo which abstracts away pointer handling.
    // Disadvantage: It requires the ILayer.IsMapInfoLayer = true.
    // Disadvantage: Cannot drag without first releasing mouse. 
    // Advantage: Faster; don't have to calculate all point distances to cursor.
    MapControl.Info += HandleInfo;
  }

  private void HandleInfo(object? sender, MapInfoEventArgs e)
  {
    if (e.MapInfo?.Layer?.Name != "GPS Editor") { return; }

    var feat = e.MapInfo?.Feature;
    if (feat == null) { return; }
    if (feat is not PointFeature pf) { return; }

    ToggleSelected(pf);
  }

  private void ToggleSelected(PointFeature? feat)
  {
    if (feat == null) { return; }

    if (feat["selected"] is null)
    { 
      feat["selected"] = "true";
      draggedPoint_ = feat;
      MapControl.Map.Navigator.PanLock = true;
      return;
    }

    feat["selected"] = null;
    draggedPoint_ = null;
    MapControl.Map.Navigator.PanLock = false;
  }

  private PointFeature? FindFeatureUnderPointer(Avalonia.Point screenPosition)
  {
    Avalonia.Point worldPosition = Viewport_
      .ScreenToWorld(screenPosition.MapMPoint())
      .MapAvaloniaPoint();

    ILayer? ilayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "GPS Editor");

    if (ilayer is not Layer layer) { return null; }
    if (layer.DataSource is not MemoryProvider mp) { return null; }

    var pointFeatures = mp.Features.Where(f => f is PointFeature).Cast<PointFeature>();
    if (pointFeatures == null) { return null; }
    
    MPoint world = worldPosition.MapMPoint();

    // Select the closest point within the given distance in meters
    double distanceMeters = 4;

    PointFeature? point = pointFeatures.Select(pf => new { pf, dist = pf.Point.Distance(world) })
      .Where(obj => obj.dist < distanceMeters)
      .OrderBy(obj => obj.dist)
      .Select(obj => obj.pf)
      .FirstOrDefault();

    return point;
  }

  private void HandlePointerMoved(object? sender, PointerEventArgs e)
  {
    if (vm_ == null) { return; }
    if (!IsDragging_) { return; }
    if (draggedPoint_ == null) { return; }

    MPoint pt = Viewport_.ScreenToWorld(e.GetPosition(MapControl).MapMPoint());
    draggedPoint_.Point.X = pt.X;
    draggedPoint_.Point.Y = pt.Y;

    MapControl.Refresh();
  }

  private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
  {
    if (vm_ == null) { return; }

    PointFeature? pf = FindFeatureUnderPointer(e.GetPosition(MapControl));
    ToggleSelected(pf);
  }

  private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
  {
    if (vm_ == null) { return; }

    ToggleSelected(draggedPoint_);
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