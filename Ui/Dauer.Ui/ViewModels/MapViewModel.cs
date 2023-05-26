using Dauer.Data.Fit;
using Dauer.Ui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;
using Mapsui.UI.Avalonia;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
using ReactiveUI;

namespace Dauer.Ui.ViewModels;

public interface IMapViewModel
{
  int SelectedIndex { get; set; }
  void ShowGpsTrace(FitFile fit);
}

public class DesignMapViewModel : MapViewModel
{
}

public class MapViewModel : ViewModelBase, IMapViewModel
{
  public IMapControl Map { get; }
  private ILayer? breadcrumbLayer_;
  private FitFile? lastFit_;

  private int selectedIndex_;
  public int SelectedIndex
  {
    get => selectedIndex_; set
    {
      if (value < 0 || value > (lastFit_?.Records.Count ?? 0)) { return; }
      this.RaiseAndSetIfChanged(ref selectedIndex_, value);
    }
  }

  public MapViewModel()
  {
    Map = new MapControl();
    Map.Map?.Layers.Add(OpenStreetMap.CreateTileLayer());

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(e => HandleSelectedIndexChanged(e.Value));
  }

  private void HandleSelectedIndexChanged(int index) => ShowCoordinate(index);

  public void ShowCoordinate(int index)
  {
    if (lastFit_ == null) { return; }

    var coord = lastFit_.Records[index].MapCoordinate();
    var circle = new GeometricShapeFactory().CreateCircle(coord, 8.0);
    var layer = new MemoryLayer
    {
      Features = new[] {new GeometryFeature { Geometry = circle } },
      Name = "Breadcrumb",
      Style = new VectorStyle
      {
#pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.
        Fill = { Color = Color.FromString("Red"), },
        Outline = { Color = Color.FromString("Blue"), Width = 2 }
      }
    };

    if (breadcrumbLayer_ != null)
    {
      Map.Map!.Layers.Remove(breadcrumbLayer_);
    }

    Map.Map!.Layers.Add(layer);
    breadcrumbLayer_ = layer;
  }

  public void ShowGpsTrace(FitFile fit)
  {
    lastFit_ = fit;

    Coordinate[] coords = fit.Records
      .Select(r => r.MapCoordinate())
      .Where(c => c.X != 0 && c.Y != 0)
      .ToArray();

    var trace = new MemoryLayer
    {
      Features = new[] { new GeometryFeature { Geometry = new LineString(coords) } },
      Name = "GPS Trace",
      Style = new VectorStyle
      {
#pragma warning disable CS8670 // Object or collection initializer implicitly dereferences possibly null member.
        Line = { Color = Color.FromString("YellowGreen"), Width = 4 }
      }
    };

    Map.Map!.Layers.Add(trace);
    Map.Map!.Home = n => n.CenterOnAndZoomTo(trace.Extent!.Centroid, 2);
    Map.Map!.Home.Invoke(Map.Map!.Navigator);
  }
}
