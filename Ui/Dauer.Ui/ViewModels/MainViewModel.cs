using Splat;
using Dauer.Ui.Adapters.Storage;
using Dauer.Services;
using ReactiveUI;
using Dauer.Data.Fit;
using System.Text;
using System.Collections.ObjectModel;
using Dauer.Model.Workouts;
using DynamicData.Binding;
using Avalonia.Controls;
using Dauer.Ui.Adapters.Windowing;
using Dauer.Model;
using ReactiveUI.Fody.Helpers;
using Avalonia;
using Dauer.Ui.Adapters;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{

}

public class DesignMainViewModel : MainViewModel
{
  public DesignMainViewModel() : base(
    new NullStorageAdapter(),
    new NullWindowAdapter(),
    new NullFitService(),
    new DesignPlotViewModel(),
    new DesignLapViewModel(),
    new DesignRecordViewModel(),
    new DesignMapViewModel(),
    new NullWebAuthenticator()
  ) 
  { 
  }
}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  private Models.File? lastFile_ = null;
  private FitFile? lastFit_ = null;
  private readonly IStorageAdapter storage_;
  private readonly IFitService fit_;
  private readonly IWebAuthenticator auth_;
  private readonly IWindowAdapter window_;

  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }

  public ObservableCollection<string> LogEntries { get; } = new();

  [Reactive]
  public string Text { get; set; } = "Welcome to FitEdit. Please load a FIT file.";

  public MainViewModel(
    IStorageAdapter storage,
    IWindowAdapter window,
    IFitService fit,
    IPlotViewModel plot,
    ILapViewModel laps,
    IRecordViewModel records,
    IMapViewModel map,
    IWebAuthenticator auth
  )
  {
    storage_ = storage;
    window_ = window;
    fit_ = fit;
    Plot = plot;
    Laps = laps;
    Records = records;
    Map = map;
    auth_ = auth;

    window_.Resized.Subscribe(async tup =>
    {
      await Log($"Window resized to {tup.Item1} {tup.Item2}");
    });

    // When the records list selection changes, show it in the plot
    records.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      plot.SelectedIndex = property.Value;
      Map.SelectedIndex = property.Value;
    });

    // When plot selected data point changes, show it in the records list
    plot.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      records.SelectedIndex = property.Value;
      Map.SelectedIndex = property.Value;
    });

    Services.Log.Info($"{nameof(MainViewModel)} ready");
  }

  public void HandleAuthorizeClicked()
  {
    Services.Log.Info($"{nameof(HandleAuthorizeClicked)}");

    auth_.AuthenticateAsync();
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

  public async void HandleSelectFileClicked()
  {
    Services.Log.Info("Select file clicked");

    // On iOS, the file picker must run on the main thread
    //_ = Task.Run(async () =>
    //{
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
          Services.Log.Info($"Unsupported extension {extension}");
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


        lastFit_ = fit;
        Show(fit);
      }
      catch (Exception e)
      {
        Services.Log.Info($"{e}");
      }
    //});
  }

  public async Task EditLapSpeeds(Speed? speed, int i)
  {
    if (lastFit_ == null)
    {
      await Log("No file loaded");
      return;
    }

    FitFile fit = lastFit_;

    await Log("Applying new lap speeds");

    fit.ApplySpeeds(new Dictionary<int, Speed?> { [i] = speed });

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
  }

  public async void HandleDownloadFileClicked()
  {
    Services.Log.Info("Download file clicked...");

    // On macOS, the file save dialog must run on the main thread
    //_ = Task.Run(async () =>
    //{
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
    //});
  }

  private void Show(FitFile fit)
  {
    Plot.Show(fit);
    Laps.Show(fit);
    Records.Show(fit);
    Map.Show(fit);

    var pairs = Laps.Laps.Select((lap, i) => new { lap.Speed, i }).ToList();

    foreach (var pair in pairs)
    {
      var speed = pair.Speed!;
      speed.WhenPropertyChanged(x => x.Value).Subscribe(async property =>
      {
        await SetLapSpeed(fit, pair.i, property.Sender);
        Plot.Show(fit);
      });
    }
  }

  private async Task SetLapSpeed(FitFile fit, int i, Speed? speed)
  {
    var spd = fit.Laps[i].GetEnhancedAvgSpeed() ?? 0;
    if (Math.Abs(speed?.Value ?? 0 - spd) < 1e-5)
    {
      return;
    }
    await EditLapSpeeds(speed, i);
  }
}
