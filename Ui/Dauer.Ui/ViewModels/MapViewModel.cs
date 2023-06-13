using Dauer.Data.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

#if USE_MAPSUI
using BruTile.Predefined;
using BruTile.Web;
using Dauer.Ui.Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
#endif

namespace Dauer.Ui.ViewModels;

public interface IMapViewModel
{
  int SelectedIndex { get; set; }
  //IMapControl? Map { get; set; }

  void Show(FitFile fit);
}

public class DesignMapViewModel : MapViewModel
{
}

#if !USE_MAPSUI
public class MapViewModel : ViewModelBase, IMapViewModel
{
  [Reactive] public int SelectedIndex { get; set; }
  public void Show(FitFile? fit) { }
}

#else
public class MapViewModel : ViewModelBase, IMapViewModel
{
  [Reactive] public IMapControl? Map { get; set; }
  private readonly GeometryFeature breadcrumbFeature_ = new();
  private ILayer BreadcrumbLayer_ => new MemoryLayer
  {
    Name = "Breadcrumb",
    Features = new[] { breadcrumbFeature_ },
    Style = new VectorStyle
    {
      Fill = new Brush(Palette.FitSkyBlue.Map()),
      Outline = new Pen(Palette.FitLeadBlack2.Map(), 4),
    }
  };

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
    this.ObservableForProperty(x => x.Map).Subscribe(e =>
    {
      // Jawg.io
      string token = "vANNdIJHPNGMEQyIhxvoWGgKQKP4kPUdaOtMDxqaNDTxere8oUgFk9vhHdHjq0n5";
      string url = $"https://tile.jawg.io/jawg-dark/{{z}}/{{x}}/{{y}}.png" +
        $"?access-token={token}";

      // MapBox
      //string token = "pk.eyJ1Ijoic2xhdGVyMCIsImEiOiJjbGllZnRwd3cxMHJxM2tuYmw4MmNtOTAzIn0.E6GxSlg70MogL-sla15bgA";
      //string url = $"https://api.mapbox.com/v4/mapbox.satellite/{{z}}/{{x}}/{{y}}@2x.png" +
      //  $"?access_token={token}";

      var source = new HttpTileSource(new GlobalSphericalMercator(), url, userAgent: "fitedit");
      var layer = new TileLayer(source) { Name = "Base Map" };

      // OpenStreetMap
      //var layer = OpenStreetMap.CreateTileLayer("fitedit");

      Map?.Map?.Layers.Add(layer); // layer 0
      Map?.Map?.Layers.Add(BreadcrumbLayer_); // layer 1
    });

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(e => HandleSelectedIndexChanged(e.Value));
  }

  private void HandleSelectedIndexChanged(int index) => ShowCoordinate(index);

  public void ShowCoordinate(int index)
  {
    if (lastFit_ == null) { return; }

    var r = lastFit_.Records[index];
    Coordinate coord = r.MapCoordinate();

    var circle = new GeometricShapeFactory { NumPoints = 16 }.CreateCircle(coord, 16.0);
    breadcrumbFeature_.Geometry = circle;
  }

  public void Show(FitFile fit)
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
        Line = new(Palette.FitLimeCrayon.Map(), 4)
      }
    };

    if (Map?.Map == null) { return; }

    Map.Map.Layers.Insert(1, trace); // Above tile layer, below breadcrumb layer
    Map.Map.Home = n => n.CenterOnAndZoomTo(trace.Extent!.Centroid, 10, 2000);
    Map.Map.Home.Invoke(Map.Map!.Navigator);
  }
}
#endif
