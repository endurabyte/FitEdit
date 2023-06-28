using Dauer.Data.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using DynamicData.Binding;
using System.Reactive;
using System.Collections.Specialized;
using Mapsui;

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
  bool HasCoordinates { get; set; }
}

public class DesignMapViewModel : MapViewModel
{
  public DesignMapViewModel() : base(new FileService())
  {

  }
}

#if !USE_MAPSUI
public class MapViewModel : ViewModelBase, IMapViewModel
{
  [Reactive] public bool HasCoordinates { get; set; }
}

#else
public class MapViewModel : ViewModelBase, IMapViewModel
{
  [Reactive] public IMapControl? Map { get; set; }
  [Reactive] public bool HasCoordinates { get; set; }

  private readonly GeometryFeature breadcrumbFeature_ = new();

  /// <summary>
  /// Key: File ID, Value: layer
  /// </summary>
  private readonly Dictionary<int, MemoryLayer> traces_ = new();

  private readonly Dictionary<SelectedFile, IDisposable> addSubs_ = new();

  private ILayer BreadcrumbLayer_ => new MemoryLayer
  {
    Name = "Breadcrumb",
    Features = new[] { breadcrumbFeature_ },
    Style = new VectorStyle
    {
      Fill = new Brush(FitColor.SkyBlue.Map()),
      Outline = new Pen(FitColor.LeadBlack2.Map(), 2),
    }
  };

  private readonly IFileService fileService_;

  public MapViewModel(
    IFileService fileService
  )
  {
    fileService_ = fileService;

    fileService.SubscribeAdds(HandleFileAdded);
    fileService.SubscribeRemoves(HandleFileRemoved);

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
  }

  private void HandleFileAdded(SelectedFile? sf)
  {
    if (sf == null) { return; }
    if (addSubs_.ContainsKey(sf)) { return; }

    addSubs_[sf] = sf.SubscribeToFitFile(HandleFitFileChanged);
    HandleFitFileChanged(sf);
  }

  private void HandleFitFileChanged(SelectedFile sf)
  {
    if (sf.Blob == null) { return; }

    // Handle file loaded
    if (sf.FitFile != null)
    {
      Show(sf.Blob.Id, sf.FitFile);
      sf.ObservableForProperty(x => x.SelectedIndex).Subscribe(e => HandleSelectedIndexChanged(e.Value));
    }
    else
    {
      // Handle file unloaded
      if (!traces_.TryGetValue(sf.Blob.Id, out MemoryLayer? trace))
      {
        return;
      }

      traces_.Remove(sf.Blob.Id);
      Map!.Map.Layers.Remove(trace);
    }

    HasCoordinates = GetHasCoordinates();
    UpdateExtent();
  }

  private void HandleFileRemoved(SelectedFile? sf)
  {
    if (sf == null) { return; }
    if (addSubs_.TryGetValue(sf, out IDisposable? sub))
    {
      sub.Dispose();
    }
    addSubs_.Remove(sf);
  }

  private void HandleSelectedIndexChanged(int index)
  {
    ShowCoordinate(fileService_.MainFile?.FitFile, index);
  }

  private void ShowCoordinate(FitFile? f, int index)
  {
    if (f == null) { return; }
    if (index < 0 || index >= f.Records.Count) { return; }

    var r = f.Records[index];
    Coordinate coord = r.MapCoordinate();

    var circle = new GeometricShapeFactory { NumPoints = 16 }.CreateCircle(coord, 16.0);
    breadcrumbFeature_.Geometry = circle;
  }

  private void Show(int id, FitFile fit, int index = -1, int count = -1)
  {
    var range = Enumerable.Range(index < 0 ? 0 : index, count < 0 ? fit.Records.Count : count);

    Coordinate[] coords = range
      .Select(i => fit.Records[i])
      .Select(r => r.MapCoordinate())
      .Where(c => c.X != 0 && c.Y != 0)
      .ToArray();

    Show(id, coords, "GPS Trace", FitColor.LimeCrayon);
  }

  private void Show(int id, Coordinate[] coords, string name, Avalonia.Media.Color color)
  { 
    if (!coords.Any()) { return; }
    if (Map?.Map == null) { return; }

    var trace = new MemoryLayer
    {
      Features = new[] { new GeometryFeature { Geometry = new LineString(coords) } },
      Name = name,
      Style = new VectorStyle
      {
        Line = new(color.Map(), 4)
      }
    };

    traces_[id] = trace;
    Map.Map.Layers.Insert(1, trace); // Above tile layer, below breadcrumb layer
  }

  private void UpdateExtent()
  {
    if (!traces_.Any()) { return; }
    if (Map == null) { return; }

    MRect extent = traces_.Values.First().Extent!;
    foreach (var t in traces_.Values)
    {
      extent = extent.Join(t.Extent);
    }

    Map.Map.Home = n => n.CenterOnAndZoomTo(extent.Centroid, 10, 2000);
    Map.Map.Home.Invoke(Map.Map!.Navigator);
  }

  private bool GetHasCoordinates() => traces_.Values.Any(trace =>
    trace.Features.Any(feat =>
      feat is GeometryFeature geom && geom.Geometry is LineString ls && !ls.IsEmpty
    ));
}

#endif
