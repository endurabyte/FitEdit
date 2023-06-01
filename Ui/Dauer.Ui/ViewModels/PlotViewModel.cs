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
  int SelectedIndex { get; set; }

  void Show(FitFile fit);
}

public class DesignPlotViewModel : PlotViewModel
{
  public DesignPlotViewModel()
  {
    Show(new FitFileFactory().CreateFake());
  }
}

public class PlotViewModel : ViewModelBase, IPlotViewModel
{
  private LineSeries? HrSeries_ => Plot?.Series[0] as LineSeries;

  private TrackerHitResult? lastTracker_;

  [Reactive]
  public PlotModel? Plot { get; set; }

  private int selectedIndex_;
  public int SelectedIndex
  {
    get => selectedIndex_; set
    {
      if (value < 0 || value > (HrSeries_?.Points.Count ?? 0)) { return; }
      this.RaiseAndSetIfChanged(ref selectedIndex_, value);
    }
  }

  [Reactive]
  public ScreenPoint? TrackerPosition { get; set; }

  public PlotViewModel()
  {
    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(e => HandleSelectedIndexChanged(e.Value));
  }

  private void HandleSelectedIndexChanged(int index)
  {
    if (lastTracker_ != null && lastTracker_.Index == index) { return; }

    LineSeries? series = HrSeries_;
    if (series == null) { return; }

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

  public void Show(FitFile fit)
  {
    var plot = new PlotModel
    {
      TextColor = OxyColors.White,
      PlotAreaBorderColor = OxyColors.Transparent,
      DefaultColors = new List<OxyColor>()
      {
        OxyColors.Red,
        OxyColors.Purple,
        OxyColor.Parse("#64b5cd00"),
      },
      TitleFontSize = 0,
      SubtitleFontSize = 0,
    };

    var controller = new PlotController();


#pragma warning disable CS0618 // Type or member is obsolete
    plot.TrackerChanged += HandleTrackerChanged;
#pragma warning restore CS0618 // Type or member is obsolete

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

    plot.Series.Add(hrSeries);
    plot.Series.Add(cadenceSeries);
    plot.Series.Add(speedSeries);

    // Axes are created automatically if they are not defined
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, IsAxisVisible = false });
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Key = "HR", IsAxisVisible = false });
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Key = "Cadence", IsAxisVisible = false });
    plot.Axes.Add(new LinearAxis { Position = AxisPosition.Right, Key = "Speed", IsAxisVisible = false });

    // Render a vertical line at the end of each lap
    foreach (int i in Enumerable.Range(0, fit.Laps.Count))
    {
      var lap = fit.Laps[i];
      double startTime = (lap.Start() - start).TotalMinutes;

      var ann = new LineAnnotation
      {
        Layer = AnnotationLayer.BelowAxes,
        Color = OxyColors.Blue,
        StrokeThickness = 1,
        LineStyle = LineStyle.Dash,
        Type = LineAnnotationType.Vertical,
        X = startTime,
        Text = $"Lap {i+1}",
        TextLinePosition = 0.1,
      };

      plot.Annotations.Add(ann);
    }

    Plot = plot;
  }

  private void HandleTrackerChanged(object? sender, TrackerEventArgs e)
  {
    if (e.HitResult == null) { return; }

    lastTracker_ = e.HitResult;
    TrackerPosition = e.HitResult.Position;
    SelectedIndex = (int)e.HitResult.Index;
  }

  public void HandleResetPlotClicked()
  {
    Plot?.PlotView.InvalidatePlot(updateData: false);
    Plot?.ResetAllAxes();
  }
}
