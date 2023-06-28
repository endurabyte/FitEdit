using ReactiveUI;
using Dauer.Data.Fit;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using OxyPlot.Axes;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IPlotViewModel
{
  void HandleWheel(double delta);
  void SelectCoordinates(double minX, double maxX);
}

public class DesignPlotViewModel : PlotViewModel
{
  public DesignPlotViewModel() : base (new FileService())
  {
    var file = new SelectedFile { FitFile = new FitFileFactory().CreateFake() };
    Add(file);
  }
}

public class PlotViewModel : ViewModelBase, IPlotViewModel
{
  private readonly RectangleAnnotation selection_ = new()
  {
      Fill = FitColor.TanCrayon.MapOxyColor(alpha: 50),
      MinimumX = 0,
      MaximumX = 0,
      MinimumY = -1,
      MaximumY = 10000
    };

  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;

  private readonly Dictionary<SelectedFile, IDisposable> isVisibleSubs_ = new();
  private readonly Dictionary<SelectedFile, List<PlotElement>> plots_ = new();

  private LineSeries? HrSeries_ => Plot?.Series[0] as LineSeries;
  private TrackerHitResult? lastTracker_;
  private double zoomScale_ = 50;

  [Reactive] public ScreenPoint? TrackerPosition { get; set; }
  [Reactive] public PlotModel? Plot { get; set; }
  [Reactive] public PlotController PlotController { get; set; } = new();

  private int selectedIndex_;
  public int SelectedIndex
  {
    get => selectedIndex_; set
    {
      if (value < 0 || value > (HrSeries_?.Points.Count ?? 0)) { return; }
      this.RaiseAndSetIfChanged(ref selectedIndex_, value);
    }
  }

  private readonly IFileService fileService_;

  public PlotViewModel
  (
    IFileService fileService
  )
  {
    fileService_ = fileService;

    CreatePlot();

    fileService.SubscribeAdds(HandleFileAdded);
    fileService.SubscribeRemoves(HandleFileRemoved);

    fileService.ObservableForProperty(x => x.MainFile).Subscribe(HandleMainFileChanged);
    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      if (fileService_?.MainFile == null ) { return; }
      fileService_.MainFile.SelectedIndex = property.Value;
    });
  }

  private void HandleMainFileChanged(IObservedChange<IFileService, SelectedFile?> property)
  {
    SelectedFile? file = property.Value;
    if (file == null) { return; }

    selectedIndexSub_?.Dispose();
    selectedCountSub_?.Dispose();

    selectedIndexSub_ = file.ObservableForProperty(x => x.SelectedIndex).Subscribe(e => HandleSelectedIndexChanged(e.Value));
    selectedCountSub_ = file.ObservableForProperty(x => x.SelectionCount).Subscribe(e => HandleSelectionCountChanged(e.Value));
  }

  private void CreatePlot()
  {
    var plot = new PlotModel
    {
      TextColor = FitColor.SnowWhite.MapOxyColor(),
      PlotAreaBorderColor = OxyColors.Transparent,
      DefaultColors = new List<OxyColor>()
      {
        FitColor.RedCrayon.MapOxyColor(),
        FitColor.PurpleCrayon.MapOxyColor(),
        FitColor.LimeCrayon.MapOxyColor(),
      },
      TitleFontSize = 0,
      SubtitleFontSize = 0,
    };

    // Axes are created automatically if they are not defined
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, IsAxisVisible = false });
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Key = "HR", IsAxisVisible = false });
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Key = "Cadence", IsAxisVisible = false });
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Right, Key = "Speed", IsAxisVisible = false });

    plot.Annotations.Add(selection_);

#pragma warning disable CS0618 // Type or member is obsolete
    plot.TrackerChanged += HandleTrackerChanged;
#pragma warning restore CS0618 // Type or member is obsolete

    Plot = plot;
  }

  private void HandleFileAdded(SelectedFile file)
  {
    // If we already have this file, don't add it again
    if (isVisibleSubs_.ContainsKey(file)) { return; }

    isVisibleSubs_[file] = file.ObservableForProperty(x => x.IsVisible).Subscribe(e => HandleFileIsVisibleChanged(e.Sender));
    HandleFileIsVisibleChanged(file);
  }

  private void HandleFileRemoved(SelectedFile file) => Remove(file);

  private void HandleFileIsVisibleChanged(SelectedFile file)
  {
    if (file.IsVisible) { Add(file); }
    else { Remove(file); }
  }

  protected void Add(SelectedFile file)
  {
    if (Plot == null) { return; }
    if (file.FitFile == null) { return; }

    FitFile fit = file.FitFile;

    // Plot heart rate and speed data points
    string str = "{0}\n{1:0.0}: {2:0.0}\n{3:0.0}: {4:0.0}";
    var hrSeries = new LineSeries { Title = "HR", YAxisKey = "HR", TrackerFormatString = str };
    var cadenceSeries = new LineSeries { Title = "Cadence", YAxisKey = "Cadence", TrackerFormatString = str };
    var speedSeries = new LineSeries { Title = "Speed", YAxisKey = "Speed", TrackerFormatString = str };

    DateTime start = fit.Sessions.First().Start();

    foreach (var record in fit.Records)
    {
      var speed = (double?)record.GetEnhancedSpeed() ?? 0;
      var hr = (double?)record.GetHeartRate() ?? 0;
      var cadence = (double?)record.GetCadence() ?? 0;
      var time = record.Start();
      double elapsedSeconds = (time - start).TotalMinutes;

      hrSeries.Points.Add(new(elapsedSeconds, hr));
      cadenceSeries.Points.Add(new(elapsedSeconds, cadence));
      speedSeries.Points.Add(new(elapsedSeconds, speed));
    }

    Plot.Series.Add(hrSeries);
    Plot.Series.Add(cadenceSeries);
    Plot.Series.Add(speedSeries);

    plots_[file] = new List<PlotElement>
    {
      hrSeries,
      cadenceSeries,
      speedSeries,
    };

    // Render a vertical line at the end of each lap
    foreach (int i in Enumerable.Range(0, fit.Laps.Count))
    {
      var lap = fit.Laps[i];
      double startTime = (lap.Start() - start).TotalMinutes;

      var ann = new LineAnnotation
      {
        Layer = AnnotationLayer.BelowAxes,
        Color = FitColor.BlueCrayon.MapOxyColor(),
        StrokeThickness = 1,
        LineStyle = LineStyle.Dash,
        Type = LineAnnotationType.Vertical,
        X = startTime,
        Text = $"Lap {i+1}",
        TextLinePosition = 0.1,
      };

      Plot.Annotations.Add(ann);
      plots_[file].Add(ann);
    }

    Redraw(true);
  }

  private void Remove(SelectedFile file)
  { 
    if (Plot == null) { return; }

    if (plots_.TryGetValue(file, out List<PlotElement>? elems)) 
    {
      foreach (var elem in elems)
      {
        if (elem is Annotation ann) { Plot.Annotations.Remove(ann); }
        if (elem is Series series) { Plot.Series.Remove(series); }
      }

      plots_.Remove(file);
    }

    Redraw(true);
  }

  private void HandleSelectedIndexChanged(int index)
  {
    SelectedIndex = index;
    if (fileService_.MainFile == null) { return; }
    fileService_.MainFile.SelectedIndex = SelectedIndex;

    if (lastTracker_ != null && lastTracker_.Index == index) { return; }

    LineSeries? series = HrSeries_;
    if (series == null) { return; }
    if (index < 0 || index >= series.Points.Count) { return; }

    DataPoint selection = series.Points[index];
    ScreenPoint position = series.Transform(selection);

    TrackerPosition = position;

    var hit = new TrackerHitResult
    {
      Position = position,
      Text = $"Record {index}"
    };

    Plot?.PlotView?.ShowTracker(hit);
  }

  private void HandleSelectionCountChanged(int count) => SelectIndices(SelectedIndex, SelectedIndex + count);

  private void HandleTrackerChanged(object? sender, TrackerEventArgs e)
  {
    if (e.HitResult == null) { return; }

    lastTracker_ = e.HitResult;
    TrackerPosition = e.HitResult.Position;
    SelectedIndex = (int)e.HitResult.Index;
  }

  public void HandleResetPlotClicked()
  {
    Redraw();
    Plot?.ResetAllAxes();
  }

  public void HandleWheel(double delta)
  {
    zoomScale_ += delta / 10;

    // -1 => wheel down, 1 => wheel up
    //double x = Plot?.Axes[0].Transform(2000) ?? 0;
    Plot?.Axes[0].Zoom(zoomScale_, 0);

    Redraw();
  }

  public void SelectCoordinates(double minX, double maxX)
  {
    selection_.MinimumX = minX;
    selection_.MaximumX = maxX;
    Redraw();
  }

  public void SelectIndices(int minX, int maxX)
  {
    if (HrSeries_ == null) { return; }

    if (maxX < minX || minX < 0 || maxX >= HrSeries_.Points.Count) 
    {
      minX = 0;
      maxX = 0;
    }

    selection_.MinimumX = HrSeries_.Points[minX].X; 
    selection_.MaximumX = HrSeries_.Points[maxX].X;

    Redraw();
  }

  private void Redraw(bool updateData = false)
  {
    if (Plot == null) { return; }
    Plot.PlotView.InvalidatePlot(updateData);
  }
}
