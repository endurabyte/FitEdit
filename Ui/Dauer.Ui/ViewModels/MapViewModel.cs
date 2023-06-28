using Dauer.Data.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using DynamicData.Binding;
using System.Reactive;
using System.Collections.Specialized;
using Mapsui;
using NetTopologySuite.Noding;
using Mapsui.Limiting;

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
  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }

  private readonly GeometryFeature breadcrumbFeature_ = new();

  /// <summary>
  /// Key: File ID, Value: layer
  /// </summary>
  private readonly Dictionary<int, MemoryLayer> traces_ = new();

  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;

  private readonly Dictionary<SelectedFile, IDisposable> isVisibleSubs_ = new();

  private ILayer BreadcrumbLayer_ => new MemoryLayer
  {
    Name = "Breadcrumb",
    Features = new[] { breadcrumbFeature_ },
    Style = new VectorStyle
    {
      Fill = new Brush(FitColor.RedCrayon.Map()),
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
    fileService.ObservableForProperty(x => x.MainFile).Subscribe(property =>
    {
      selectedIndexSub_?.Dispose();
      selectedCountSub_?.Dispose();

      selectedIndexSub_ = property.Value.ObservableForProperty(x => x.SelectedIndex).Subscribe(prop => SelectedIndex = prop.Value);
      selectedCountSub_ = property.Value.ObservableForProperty(x => x.SelectionCount).Subscribe(prop => SelectionCount = prop.Value);
    });

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

      double extent = 20037508; // Mercator projection world extent
      var canvas = new MemoryLayer 
      {
        Name = "Canvas",
        Features = new[] { new GeometryFeature { Geometry = new Polygon(new LinearRing(new[]
        {
            new Coordinate(-extent, -extent),
            new Coordinate(extent, -extent),
            new Coordinate(extent, extent),
            new Coordinate(-extent, extent),
            new Coordinate(-extent, -extent),
        }))}},
        Style = new VectorStyle { Fill = new Brush { Color = FitColor.LicoriceBlack.Map() } },
      };

      Map?.Map?.Layers.Add(canvas);
      Map?.Map?.Layers.Add(layer);
      Map!.Map.Navigator.Limiter = new ViewportLimiterKeepWithinExtent();
    });

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(prop => HandleSelectedIndexChanged(prop.Value));
    this.ObservableForProperty(x => x.SelectionCount).Subscribe(prop => ShowSelection());
  }

  private void ShowSelection()
  {
    int magic = 101;
    if (traces_.TryGetValue(magic, out MemoryLayer? value))
    {
      traces_.Remove(magic);
      Map!.Map.Layers.Remove(value);
    }

    FitFile? file = fileService_.MainFile?.FitFile;

    if (file == null) { return; }
    if (SelectionCount < 2) { return; } // Need at least 2 points selected to draw a line between them
    if (SelectedIndex + SelectionCount >= file.Records.Count) { return; }

    Add(magic, file, "Selection", FitColor.RedCrayon, lineWidth: 6, SelectedIndex, SelectionCount);
  }

  private void HandleFileAdded(SelectedFile? sf) => Add(sf);

  private void Add(SelectedFile? sf)
  { 
    if (sf == null) { return; }
    if (isVisibleSubs_.ContainsKey(sf)) { isVisibleSubs_[sf].Dispose(); }

    isVisibleSubs_[sf] = sf.ObservableForProperty(x => x.IsVisible).Subscribe(e => HandleFileIsVisibleChanged(e.Sender));

    HandleFitFileChanged(sf);
  }

  private void HandleFileIsVisibleChanged(SelectedFile file)
  {
    if (file.IsVisible) { Add(file); }
    else { Remove(file); }
  }

  private void HandleFitFileChanged(SelectedFile sf)
  {
    if (sf.Blob == null) { return; }

    // Handle file loaded
    if (sf.FitFile != null)
    {
      Add(sf.Blob.Id, sf.FitFile, "GPS Trace", FitColor.LimeCrayon);
    }
    else
    {
      Remove(sf);
    }

    HasCoordinates = GetHasCoordinates();
    UpdateExtent();
  }

  private void HandleFileRemoved(SelectedFile? sf) => Remove(sf);

  private void Remove(SelectedFile? sf)
  { 
    if (sf == null) { return; }
    if (sf.Blob == null) { return; }

    if (!traces_.TryGetValue(sf.Blob.Id, out MemoryLayer? trace))
    {
      return;
    }

    traces_.Remove(sf.Blob.Id);
    Map!.Map.Layers.Remove(trace);
  }

  private void HandleSelectedIndexChanged(int index)
  {
    SelectionCount = 0;
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

  private void Add(int id, FitFile fit, string name, Avalonia.Media.Color color, int lineWidth = 4, int index = -1, int count = -1)
  {
    var range = Enumerable.Range(index < 0 ? 0 : index, count < 0 ? fit.Records.Count : count);

    Coordinate[] coords = range
      .Select(i => fit.Records[i])
      .Select(r => r.MapCoordinate())
      .Where(c => c.X != 0 && c.Y != 0)
      .ToArray();

    Add(id, coords, name, color, lineWidth);
  }

  private void Add(int id, Coordinate[] coords, string name, Avalonia.Media.Color color, int lineWidth)
  { 
    if (coords.Length < 2) { return; }
    if (Map?.Map == null) { return; }

    var trace = new MemoryLayer
    {
      Features = new[] { new GeometryFeature { Geometry = new LineString(coords) } },
      Name = name,
      Style = new VectorStyle
      {
        Line = new(color.Map(), lineWidth)
      }
    };

    traces_[id] = trace;
    Map.Map.Layers.Remove(BreadcrumbLayer_);
    Map.Map.Layers.Add(trace); // Above tile layer
    Map.Map.Layers.Add(BreadcrumbLayer_);
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

    Map.Map.Home = n => n.CenterOnAndZoomTo(extent.Centroid, 4, 1000);
    Map.Map.Home.Invoke(Map.Map!.Navigator);
  }

  private bool GetHasCoordinates() => traces_.Values.Any(trace =>
    trace.Features.Any(feat =>
      feat is GeometryFeature geom && geom.Geometry is LineString ls && !ls.IsEmpty
    ));
}

#endif
