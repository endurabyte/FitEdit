using ReactiveUI;
using Dauer.Data.Fit;
using OxyPlot;
using OxyPlot.Annotations;

namespace Dauer.Ui.ViewModels;

public interface IPlotViewModel
{
  void Show(FitFile fit);
}

public class PlotViewModel : ViewModelBase, IPlotViewModel
{
  private PlotModel? model_;

  public PlotModel? Plot
  {
    get => model_;
    private set => this.RaiseAndSetIfChanged(ref model_, value);
  }

  private ScreenPoint? trackerPosition_;
  public ScreenPoint? TrackerPosition
  {
    get => trackerPosition_;
    set => this.RaiseAndSetIfChanged(ref trackerPosition_, value);
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

#pragma warning disable CS0618 // Type or member is obsolete
    plot.TrackerChanged += HandleTrackerChanged;
#pragma warning restore CS0618 // Type or member is obsolete

    // Plot heart rate and speed data points
    string str = "{0}\n{1:0.0}: {2:0.0}\n{3:0.0}: {4:0.0}";
    var hrSeries = new OxyPlot.Series.LineSeries { Title = "HR", YAxisKey = "HR", TrackerFormatString = str };
    var cadenceSeries = new OxyPlot.Series.LineSeries { Title = "Cadence", YAxisKey = "Cadence", TrackerFormatString = str };
    var speedSeries = new OxyPlot.Series.LineSeries { Title = "Speed", YAxisKey = "Speed", TrackerFormatString = str };

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
    plot.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Bottom, IsAxisVisible = false });
    plot.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Key = "HR", IsAxisVisible = false });
    plot.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Key = "Cadence", IsAxisVisible = false });
    plot.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Right, Key = "Speed", IsAxisVisible = false });

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

    // Set the Model property. INotifyPropertyChanged will update the PlotView
    Plot = plot;
  }

  private void HandleTrackerChanged(object? sender, TrackerEventArgs e)
  {
    TrackerPosition = e.HitResult?.Position;
  }

  public void HandleResetPlotClicked()
  {
    Plot?.PlotView.InvalidatePlot(updateData: false);
    Plot?.ResetAllAxes();
  }
}
