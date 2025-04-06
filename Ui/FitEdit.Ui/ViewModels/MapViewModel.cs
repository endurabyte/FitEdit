﻿using FitEdit.Data.Fit;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using FitEdit.Model.Data;
using FitEdit.Model;
using FitEdit.Data;
using FitEdit.Ui.Extensions;
using FitEdit.Ui.Models;
using Mapsui.Providers;


#if USE_MAPSUI
using Mapsui;
using Mapsui.Limiting;
using Mapsui.Tiling;
using FitEdit.Ui.Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
#endif

namespace FitEdit.Ui.ViewModels;

public interface IMapViewModel
{
  bool HasCoordinates { get; set; }
  IMapControl? Map { get; set; }
}

public class DesignMapViewModel : MapViewModel
{
  public DesignMapViewModel() : base(new NullFileService(), new NullDatabaseAdapter(), TileSource.Jawg)
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
  [Reactive] public MapState State { get; set; } = MapState.Viewing;
  public bool IsViewing => State == MapState.Viewing;
  public bool IsEditing => State == MapState.Editing;
  public bool IsSaving => State == MapState.Saving;
  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }

  private readonly GeometryFeature breadcrumbFeature_ = new();

  /// <summary>
  /// Key: File ID, Value: layer index
  /// </summary>
  private readonly Dictionary<string, int> traceIndices_ = new();

  /// <summary>
  /// Key: File ID, Value: layer
  /// </summary>
  private readonly Dictionary<string, ILayer> traces_ = new();

  /// <summary>
  /// Key: Layer index, Value: layer
  /// </summary>
  private readonly Dictionary<int, ILayer> layers_ = new();

  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;

  private readonly Dictionary<UiFile, IDisposable> isLoadedSubs_ = new();

  private int canvasLayerIndex_ = 0; 
  private int tileLayerIndex_ = 1; 
  private int traceLayerIndex_ = 2; // Index of the first GPS trace. There is one for each loaded file.
  private int SelectionLayerIndex_ => traceLayerIndex_ + 1;
  private int EditLayerIndex_ => SelectionLayerIndex_ + 1;
  private int BreadcrumbLayerIndex_ => EditLayerIndex_ + 1;
  private string selectionTraceId_ = "selection-trace";
  private string editTraceId_ = "edit-trace";

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

  private readonly Queue<UiFile> queue_ = new();
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
    foreach (var file in fileService.Files.ToList()) // create copy since items may be added while iterating
    {
      HandleFileAdded(file);
    }
    this.ObservableForProperty(x => x.Map).Subscribe(e => HandleMapControlChanged());
    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(prop => HandleSelectedIndexChanged(prop.Value));
    this.ObservableForProperty(x => x.SelectionCount).Subscribe(prop => ShowSelection());
    
    this.ObservableForProperty(x => x.State).Subscribe(prop => this.RaisePropertyChanged(nameof(IsViewing)));
    this.ObservableForProperty(x => x.State).Subscribe(prop => this.RaisePropertyChanged(nameof(IsEditing)));
    this.ObservableForProperty(x => x.State).Subscribe(prop => this.RaisePropertyChanged(nameof(IsSaving)));
  }

  public void HandleEditClicked() => StartEditing();
  public void HandleCancelClicked() => EndEditing();

  public async Task HandleSaveClicked()
  {
    try
    {
      UiFile? uif = fileService_.MainFile;
      if (uif == null) { return; }

      bool gotLayer = traces_.TryGetValue(editTraceId_, out ILayer? traceLayer);
      if (!gotLayer) { return; }

      await Task.Run(() =>
      {
        State = MapState.Saving;
        CommitEdits(traceLayer, uif);
      });
      
      Remove(uif);
      Show(uif);
      
      await fileService_.UpdateAsync(uif.Activity);

      // Reload changes
      fileService_.MainFile = null;
      fileService_.MainFile = uif;
    }
    finally
    {
      EndEditing();
    }
  }

  private void StartEditing()
  {
    State = MapState.Editing;

    UiFile? uif = fileService_.MainFile;
    if (uif?.FitFile == null) { return; }

    ILayer? layer = AddTrace(uif.FitFile, "GPS Editor", EditLayerIndex_, FitColor.LimeCrayon, editable: true);

    if (layer == null) { return; }
    traces_[editTraceId_] = layer;
    layers_[EditLayerIndex_] = layer;

    Remove(uif); // Calls HandleLayersChanged
  }

  private void EndEditing()
  {
    traces_.Remove(editTraceId_);
    layers_.Remove(EditLayerIndex_);
    HandleLayersChanged();
    
    State = MapState.Viewing;
  }

  /// <summary>
  /// Commit changed GPS coordinates to current FIT file
  /// </summary>
  private static void CommitEdits(ILayer? traceLayer, UiFile file)
  {
    if (file.FitFile is not { } fit) { return; }
    
    if (traceLayer is not Layer layer) { return; }
    if (layer.DataSource is not MemoryProvider mp) { return; }

    Coordinate[] coords = mp.Features
      .Cast<PointFeature>()
      .Select(f => f.Point.MapCoordinate())
      .ToArray();

    if (coords.Length != fit.Records.Count) { return; }

    foreach (var pair in fit.Records.Select((record, i) => new { record, i }))
    {
      pair.record.SetCoordinate(coords[pair.i]);
    }

    fit.BackfillEvents();
    file.Commit(fit);
  }

  private void HandleMainFileChanged(IObservedChange<IFileService, UiFile?> property)
  {
    selectedIndexSub_?.Dispose();
    selectedCountSub_?.Dispose();

    selectedIndexSub_ = property.Value.ObservableForProperty(x => x.SelectedIndex)
      .Subscribe(prop => SelectedIndex = prop.Value);
    selectedCountSub_ = property.Value.ObservableForProperty(x => x.SelectionCount)
      .Subscribe(prop => SelectionCount = prop.Value);

    EndEditing();
  }

  private void HandleMapControlChanged()
  {
    if (Map?.Map?.Layers == null) { return; }

    // TODO move to infrastructure
    LayerFactory.DefaultCache = new PersistentCache($"{tileSource_}", db_);
    OpenStreetMap.DefaultCache = LayerFactory.DefaultCache;
    
    layers_[canvasLayerIndex_] = LayerFactory.CreateCanvas();
    layers_[tileLayerIndex_] = LayerFactory.CreateTileLayer(tileSource_);
    layers_[BreadcrumbLayerIndex_] = BreadcrumbLayer_;

    HandleLayersChanged();

    Map.Map.Navigator.Limiter = new ViewportLimiterKeepWithinExtent();

    while (queue_.Count > 0)
    {
      Show(queue_.Dequeue());
    }

    UpdateExtent();
    HasCoordinates = LayerFactory.GetHasCoordinates(traces_.Values);
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
      layers_.Remove(SelectionLayerIndex_);
      traces_.Remove(selectionTraceId_);
      HandleLayersChanged();
    }

    FitFile? file = fileService_.MainFile?.FitFile;

    if (file == null) { return; }
    if (SelectionCount < 2) { return; } // Need at least 2 points selected to draw a line between them
    if (SelectedIndex + SelectionCount >= file.Records.Count) { return; }

    ILayer? layer = AddTrace(file, "Selection", SelectionLayerIndex_, FitColor.RedCrayon, editable: false, lineWidth: 6, index: SelectedIndex, count: SelectionCount);

    if (layer == null) { return; }
    traces_[selectionTraceId_] = layer;
    layers_[SelectionLayerIndex_] = layer;

    HandleLayersChanged();
  }

  private void HandleFileAdded(UiFile? sf) => SubscribeLoads(sf);

  private void SubscribeLoads(UiFile? sf)
  { 
    if (sf == null) { return; }
    
    if (isLoadedSubs_.TryGetValue(sf, out var sub)) { sub.Dispose(); }

    isLoadedSubs_[sf] = sf.ObservableForProperty(x => x.IsLoaded)
      .Subscribe(e => HandleFileIsLoadedChanged(e.Sender));
  }

  private void HandleFileIsLoadedChanged(UiFile file)
  {
    if (file.IsLoaded) { TryShow(file); }
    else { Remove(file); }
  }

  private void TryShow(UiFile uif)
  {
    if (Map?.Map == null)
    {
      queue_.Enqueue(uif);
      return;
    }

    Show(uif);
    UpdateExtent();
    HasCoordinates = LayerFactory.GetHasCoordinates(traces_.Values);
  }

  private void Show(UiFile sf)
  {
    if (sf.Activity == null) { return; }

    // Handle file loaded
    if (sf.FitFile != null)
    {
      int index = traceLayerIndex_ + traces_.Count;
      ILayer? layer = AddTrace(sf.FitFile, "GPS Trace", index, FitColor.LimeCrayon);

      if (layer == null) { return; }
      traceIndices_[sf.Activity.Id] = index;
      traces_[sf.Activity.Id] = layer;
      layers_[index] = layer;

      HandleLayersChanged();
    }
    else
    {
      Remove(sf);
    }
  }

  private void HandleFileRemoved(UiFile? sf) => Remove(sf);

  private void Remove(UiFile? sf)
  { 
    if (sf == null) { return; }
    if (sf.Activity == null) { return; }

    if (traceIndices_.TryGetValue(sf.Activity.Id, out int index))
    {
      layers_.Remove(index);
    }

    traceIndices_.Remove(sf.Activity.Id);
    traces_.Remove(sf.Activity.Id);

    HandleLayersChanged();
  }

  private void HandleSelectedIndexChanged(int index)
  {
    if (Map?.Map is null) { return; }
    SelectionCount = 0;
    ShowCoordinate(fileService_.MainFile?.FitFile, index);
    Map.Map.RefreshGraphics();
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

  private ILayer? AddTrace
  (
    FitFile fit,
    string name,
    int layer,
    Avalonia.Media.Color color,
    bool editable = false,
    int lineWidth = 4,
    int index = -1,
    int count = -1
  )
  {
    var range = Enumerable
      .Range(index < 0 ? 0 : index, count < 0 ? fit.Records.Count : count)
      .ToList();

    Coordinate[] coords = range
      .Select(i => fit.Records[i])
      .Select(r => r.MapCoordinate())
      .ToArray();

    // Prevent zero lat lon
    foreach (int i in range.Skip(1))
    {
      PreventZeroLatLon(coords, i);
    }
    
    return editable 
      ? AddEditTrace(coords, name, color, FitColor.RedCrayon) 
      : AddTrace(coords, name, layer, color, lineWidth);
  }

  private static void PreventZeroLatLon(Coordinate[] coords, int i)
  {
    Coordinate coord = coords[i];
    Coordinate prev = coords[i - 1];

    const double tol = 1e-6;
    if (Math.Abs(coord.X) < tol && Math.Abs(coord.Y) < tol)
    {
      coord.X = prev.X;
      coord.Y = prev.Y;
    }
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

    var trace = LayerFactory.CreatePointFeatures(coords, name, color, selectedColor, 0.5);

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
