using Splat;
using Dauer.Ui.Adapters.Storage;
using Dauer.Services;
using ReactiveUI;
using Dauer.Data.Fit;
using System.Text;
using System.Collections.ObjectModel;
using Dauer.Model.Workouts;
using OxyPlot;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{

}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  private Models.File? lastFile_ = null;
  private FitFile? lastFit_ = null;
  private readonly IStorageAdapter storage_;
  private readonly IFitService fit_;
  private string text_ = "Welcome to Dauer. Please load a FIT file.";
  private PlotModel? model_;

  public ObservableCollection<string> LogEntries { get; } = new();


  public string Text
  {
    get => text_;
    set => this.RaiseAndSetIfChanged(ref text_, value);
  }

  public PlotModel? Model
  {
    get => model_;
    private set => this.RaiseAndSetIfChanged(ref model_, value);
  }

  public MainViewModel() : this(new NullStorageAdapter(), new NullFitService()) { }

  public MainViewModel(IStorageAdapter storage, IFitService fit)
  {
    storage_ = storage;
    fit_ = fit;
    this.Log().Debug($"{nameof(MainViewModel)}.ctor");
    Services.Log.Info($"{nameof(MainViewModel)} ready");

    Model = new PlotModel();
  }

  private void Plot(FitFile fit)
  {
    // Create the plot model
    var tmp = new PlotModel
    {
      Title = "Title",
      Subtitle = "Subtitle",
      TextColor = OxyColors.White,
      TitleColor = OxyColors.White,
      SubtitleColor = OxyColors.White,
      PlotAreaBorderColor = OxyColors.White,
      DefaultColors = new List<OxyColor>()
      {
        OxyColors.Red,
        OxyColors.Green,
        OxyColor.Parse("#64b5cd00"),
      },
    };

    // Create two line series (markers are hidden by default)
    var hrSeries = new OxyPlot.Series.LineSeries { Title = "HR", YAxisKey = "HR", MarkerType = MarkerType.Square };
    var speedSeries = new OxyPlot.Series.LineSeries { Title = "Speed", YAxisKey = "Speed", MarkerType = MarkerType.Square };

    DateTime start = fit.Sessions.First().GetStartTime().GetDateTime();
    foreach (var record in fit.Records)
    {
      var speed = (double?)record.GetEnhancedSpeed() ?? 0;
      var hr = (double?)record.GetHeartRate() ?? 0;
      var time = record.GetTimestamp().GetDateTime();
      double elapsedSeconds = (time - start).TotalSeconds;

      hrSeries.Points.Add(new(elapsedSeconds, hr));
      speedSeries.Points.Add(new(elapsedSeconds, speed));
    }

    tmp.Series.Add(hrSeries);
    tmp.Series.Add(speedSeries);

    // Axes are created automatically if they are not defined
    tmp.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Left, Key="HR" });
    tmp.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = OxyPlot.Axes.AxisPosition.Right, Key="Speed" });

    // Set the Model property. INotifyPropertyChanged will update the PlotView
    Model = tmp;
    //Model.ResetAllAxes();
  }

  private async Task Log(string s)
  {
    Services.Log.Info(s);
    LogEntries.Add(s);
    while (LogEntries.Count > 25) RemoveHead();

    // Give other jobs a chance to run on single-threaded platforms like WASM
    await Task.Delay(1);
  }

  private void RemoveHead() => LogEntries.RemoveAt(0);
  private void RemoveTail()
  {
    if (LogEntries.Count > 0)
    {
      LogEntries.RemoveAt(LogEntries.Count - 1);
    }
  }

  public void HandleSelectFileClicked()
  {
    Services.Log.Info("Select file clicked");

    _ = Task.Run(async () =>
    {
      try
      {
        Models.File? file = await storage_.OpenFileAsync();
        if (file == null)
        {
          Services.Log.Info("Could not load file");
          return;
        }
        Services.Log.Info($"Got file {file.Name} ({file.Bytes.Length} bytes)");
        lastFile_ = file;

        // Handle FIT files
        string extension = Path.GetExtension(file.Name);

        if (extension.ToLower() != ".fit")
        {
          return;
        }

        using var ms = new MemoryStream(lastFile_.Bytes);
        await Log($"Reading FIT file {file.Name}");
        await Log($"Read progress: ");

        var reader = new Reader();
        if (!reader.TryGetDecoder(file.Name, ms, out FitFile fit, out var decoder))
        {
          return;
        }

        long lastPosition = 0;
        long resolution = 5 * 1024; // report progress every 5 kB

        // Instead of reading all FIT messages at once,
        // Read just a few FIT messages at a time so that other tasks can run on the main thread e.g. in WASM
        while (await reader.ReadOneAsync(ms, decoder, 100))
        {
          if (ms.Position - resolution > lastPosition)
          {
            continue;
          }

          RemoveTail();
          string percent = $"{(double)ms.Position / ms.Length * 100:##.##}";
          await Log($"Reading...{percent}% ({ms.Position}/{ms.Length})");
          lastPosition = ms.Position;
        }

        RemoveTail();
        await Log($"Read progress: 100%");

        var sb = new StringBuilder();
        fit.Print(s => sb.AppendLine(s), showRecords: false);
        await Log(sb.ToString());
        Plot(fit);
        lastFit_ = fit;
      }
      catch (Exception e)
      {
        Services.Log.Info($"{e}");
      }
    });
  }

  public void HandleProcessFileClicked()
  {
    _ = Task.Run(async () =>
    {
      if (lastFit_ == null)
      {
        await Log("No file loaded");
        return;
      }

      FitFile fit = lastFit_;

      var speeds = new List<Speed>
      {
        new() { Value = 6.7, Unit = Dauer.Model.Units.SpeedUnit.MiPerHour },
        new() { Value = 9, Unit = Dauer.Model.Units.SpeedUnit.MiPerHour },
        new() { Value = 5, Unit = Dauer.Model.Units.SpeedUnit.MiPerHour },
        new() { Value = 9, Unit = Dauer.Model.Units.SpeedUnit.MiPerHour },
        new() { Value = 5, Unit = Dauer.Model.Units.SpeedUnit.MiPerHour },
        new() { Value = 6.7, Unit = Dauer.Model.Units.SpeedUnit.MiPerHour },
      };

      await Log("Applying new lap speeds");

      fit.ApplySpeeds(speeds);

      await Log("Backfilling: ");

      fit.BackfillEvents(100, async (i, total) =>
      {
        RemoveTail();
        await Log($"Backfilling: {(double)i / total * 100:##.##}% ({i}/{total})");
      });
      RemoveTail();
      await Log("Backfilling: 100%");

      var sb = new StringBuilder();
      fit.Print(s => sb.AppendLine(s), showRecords: false);
      await Log(sb.ToString());
      Plot(fit);
    });
  }

  public void HandleDownloadFileClicked()
  {
    Services.Log.Info("Download file clicked...");

    _ = Task.Run(async () =>
    {
      try
      {
        if (lastFile_ == null)
        {
          await Log("Cannot download file; none has been uploaded");
          return;
        }

        string name = Path.GetFileNameWithoutExtension(lastFile_.Name);
        string extension = Path.GetExtension(lastFile_.Name);
        await storage_.SaveAsync(new Models.File($"{name}_edit.{extension}", lastFile_.Bytes));
      }
      catch (Exception e)
      {
        Services.Log.Info($"{e}");
      }
    });
  }
}
