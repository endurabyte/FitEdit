using Dauer.Data.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using Dauer.Model.Data;
using Dauer.Model;
using Mapsui.UI.Avalonia;

#if USE_MAPSUI
using Mapsui;
using Mapsui.Limiting;
using Mapsui.Tiling;
using Dauer.Ui.Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
#endif

namespace Dauer.Ui.ViewModels;

public interface IMapViewModel
{
  bool HasCoordinates { get; set; }
  IMapControl? Map { get; set; }
}

public class DesignMapViewModel : MapViewModel
{
  public DesignMapViewModel() : base(new FileService(), new NullDatabaseAdapter(), TileSource.Jawg)
  {

  }
}

#if !USE_MAPSUI
public class MapViewModel : ViewModelBase, IMapViewModel
{
  [Reactive] public bool HasCoordinates { get; set; }

  public MapViewModel
  (
    IFileService fileService,
    IDatabaseAdapter db,
    TileSource tileSource
  )
  {

  }
}

#else
public class MapViewModel : ViewModelBase, IMapViewModel
{
  [Reactive] public IMapControl? Map { get; set; }
  [Reactive] public bool HasCoordinates { get; set; }
  [Reactive] public bool Editing { get; set; }
  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }

  private readonly GeometryFeature breadcrumbFeature_ = new();

  /// <summary>
  /// Key: File ID, Value: layer
  /// </summary>
  private readonly Dictionary<long, ILayer> traces_ = new();

  /// <summary>
  /// Key: Layer index, Value: layer
  /// </summary>
  private readonly Dictionary<int, ILayer> layers_ = new();

  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;

  private readonly Dictionary<UiFile, IDisposable> isVisibleSubs_ = new();

  private int canvasLayerIndex_ = 0; 
  private int tileLayerIndex_ = 1; 
  private int traceLayerIndex_ = 2; 
  private int selectionLayerIndex_ = 3; 
  private int editLayerIndex_ = 4; 
  private int breadcrumbLayerIndex_ = 5; 
  private int selectionTraceId_ = 101;
  private int editTraceId_ = 200;

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
  private readonly IDatabaseAdapter db_;
  private readonly TileSource tileSource_;

  public MapViewModel
  (
    IFileService fileService,
    IDatabaseAdapter db,
    TileSource tileSource
  )
  {
    fileService_ = fileService;
    db_ = db;
    tileSource_ = tileSource;

    fileService.SubscribeAdds(HandleFileAdded);
    fileService.SubscribeRemoves(HandleFileRemoved);
    fileService.ObservableForProperty(x => x.MainFile).Subscribe(HandleMainFileChanged);
    this.ObservableForProperty(x => x.Map).Subscribe(e => HandleMapControlChanged());
    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(prop => HandleSelectedIndexChanged(prop.Value));
    this.ObservableForProperty(x => x.SelectionCount).Subscribe(prop => ShowSelection());
    this.ObservableForProperty(x => x.Editing).Subscribe(e => HandleEditingChanged());
  }

  private void HandleEditingChanged()
  {
    traces_.Remove(editTraceId_);
    layers_.Remove(editLayerIndex_);
    HandleLayersChanged();

    if (!Editing)
    {
      return;
    }
    
    UiFile? sf = fileService_.MainFile;
    if (sf == null) { return; }
    if (sf.Blob == null) { return; }
    if (sf.FitFile == null) { return; }

    ILayer? layer = Add(sf.FitFile, "GPS Editor", editLayerIndex_, FitColor.LimeCrayon, editable: true);

    if (layer == null) { return; }
    traces_[editTraceId_] = layer;
    layers_[editLayerIndex_] = layer;

    HandleLayersChanged();
  }

  private void HandleMainFileChanged(IObservedChange<IFileService, UiFile?> property)
  {
    selectedIndexSub_?.Dispose();
    selectedCountSub_?.Dispose();

    selectedIndexSub_ = property.Value.ObservableForProperty(x => x.SelectedIndex).Subscribe(prop => SelectedIndex = prop.Value);
    selectedCountSub_ = property.Value.ObservableForProperty(x => x.SelectionCount).Subscribe(prop => SelectionCount = prop.Value);

    Editing = false;
  }

  private void HandleMapControlChanged()
  {
    if (Map?.Map?.Layers == null) { return; }

    // TODO move to infrastructure
    LayerFactory.DefaultCache = new PersistentCache($"{tileSource_}", db_);
    OpenStreetMap.DefaultCache = LayerFactory.DefaultCache;
    
    layers_[canvasLayerIndex_] = LayerFactory.CreateCanvas();
    layers_[tileLayerIndex_] = LayerFactory.CreateTileLayer(tileSource_);
    layers_[breadcrumbLayerIndex_] = BreadcrumbLayer_;

    HandleLayersChanged();

    Map.Map.Navigator.Limiter = new ViewportLimiterKeepWithinExtent();
  }

  // Sort layers, update map
  private void HandleLayersChanged()
  {
    if (Map?.Map == null) { return; }

    Map.Map.Layers.Clear();

    List<int> indices = layers_.Keys.Order().ToList();
    foreach (var index in indices)
    {
      Map.Map.Layers.Add(layers_[index]);
    }

    Map.Map.Refresh();
  }

  private void ShowSelection()
  {
    if (traces_.TryGetValue(selectionTraceId_, out ILayer? value))
    {
      traces_.Remove(selectionTraceId_);
      layers_.Remove(selectionTraceId_);
      HandleLayersChanged();
    }

    FitFile? file = fileService_.MainFile?.FitFile;

    if (file == null) { return; }
    if (SelectionCount < 2) { return; } // Need at least 2 points selected to draw a line between them
    if (SelectedIndex + SelectionCount >= file.Records.Count) { return; }

    ILayer? layer = Add(file, "Selection", selectionLayerIndex_, FitColor.RedCrayon, editable: false, lineWidth: 6, index: SelectedIndex, count: SelectionCount);

    if (layer == null) { return; }
    traces_[selectionTraceId_] = layer;
    layers_[selectionLayerIndex_] = layer;

    HandleLayersChanged();
  }

  private void HandleFileAdded(UiFile? sf) => Add(sf);

  private void Add(UiFile? sf)
  { 
    if (sf == null) { return; }
    if (isVisibleSubs_.ContainsKey(sf)) { isVisibleSubs_[sf].Dispose(); }

    isVisibleSubs_[sf] = sf.ObservableForProperty(x => x.IsVisible).Subscribe(e => HandleFileIsVisibleChanged(e.Sender));

    HandleFitFileChanged(sf);
  }

  private void HandleFileIsVisibleChanged(UiFile file)
  {
    if (file.IsVisible) { Add(file); }
    else { Remove(file); }

    HasCoordinates = LayerFactory.GetHasCoordinates(traces_.Values);
  }

  private void HandleFitFileChanged(UiFile sf)
  {
    if (sf.Blob == null) { return; }

    // Handle file loaded
    if (sf.FitFile != null)
    {
      ILayer? layer = Add(sf.FitFile, "GPS Trace", traceLayerIndex_, FitColor.LimeCrayon);

      if (layer == null) { return; }
      traces_[sf.Blob.Id] = layer;
      layers_[traceLayerIndex_] = layer;

      HandleLayersChanged();
    }
    else
    {
      Remove(sf);
    }

    HasCoordinates = LayerFactory.GetHasCoordinates(traces_.Values);
    UpdateExtent();
  }

  private void HandleFileRemoved(UiFile? sf) => Remove(sf);

  private void Remove(UiFile? sf)
  { 
    if (sf == null) { return; }
    if (sf.Blob == null) { return; }

    if (!traces_.TryGetValue(sf.Blob.Id, out ILayer? trace))
    {
      return;
    }

    traces_.Remove(sf.Blob.Id);
    layers_.Remove(traceLayerIndex_);
    HandleLayersChanged();
  }

  private void HandleSelectedIndexChanged(int index)
  {
    SelectionCount = 0;
    ShowCoordinate(fileService_.MainFile?.FitFile, index);
    Map!.Map.RefreshGraphics();
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

  private ILayer? Add(
    FitFile fit,
    string name,
    int layer,
    Avalonia.Media.Color color,
    bool editable = false,
    int lineWidth = 4,
    int index = -1,
    int count = -1)
  {
    var range = Enumerable.Range(index < 0 ? 0 : index, count < 0 ? fit.Records.Count : count);

    Coordinate[] coords = range
      .Select(i => fit.Records[i])
      .Select(r => r.MapCoordinate())
      .Where(c => c.X != 0 && c.Y != 0)
      .ToArray();

    return editable 
      ? AddEditTrace(coords, name, color, FitColor.RedCrayon) 
      : AddTrace(coords, name, layer, color, lineWidth);
  }

  private ILayer? AddTrace(Coordinate[] coords, string name, int layer, Avalonia.Media.Color color, int lineWidth)
  { 
    if (coords.Length < 2) { return null; }
    if (Map?.Map == null) { return null; }

    var trace = LayerFactory.CreateLineString(coords, name, color, lineWidth);

    Map.Map.Layers.Insert(layer, trace);
    return trace;
  }

  private ILayer? AddEditTrace(Coordinate[] coords, string name, Avalonia.Media.Color color, Avalonia.Media.Color selectedColor)
  {
    if (coords.Length < 2) { return null; }
    if (Map?.Map == null) { return null; }

    var trace = LayerFactory.CreatPointFeatures(coords, name, color, selectedColor, 0.5);

    return trace;
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
}

#endif
