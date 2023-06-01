using System.Collections.ObjectModel;
using System.Text;
using Dauer.Data.Fit;
using Dauer.Model.Workouts;
using Dauer.Services;
using Dauer.Ui.Adapters.Storage;
using Dauer.Ui.Adapters.Windowing;
using Dauer.Ui.Extensions;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

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
  private readonly IStorageAdapter storage_;
  private readonly IFitService fit_;
  private readonly IWebAuthenticator auth_;
  private readonly IWindowAdapter window_;

  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }

  public ObservableCollection<string> LogEntries { get; } = new();

  [Reactive] public FitFile? FitFile { get; set; }
  [Reactive] public string Text { get; set; } = "Welcome to FitEdit. Please load a FIT file.";
  [Reactive] public double Progress { get; set; }
  [Reactive] public int SliderValue { get; set; }
  [Reactive] public int SliderMax { get; set; }

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
      SliderValue = property.Value;
    });

    // When plot selected data point changes, show it in the records list
    plot.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      records.SelectedIndex = property.Value;
      Map.SelectedIndex = property.Value;
      SliderValue = property.Value;
    });

    laps.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      FitFile = property.Value;
    });

    this.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      Show(FitFile);
    });

    this.ObservableForProperty(x => x.SliderValue).Subscribe(property =>
    {
      plot.SelectedIndex = property.Value;
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

    await TaskHelp.MaybeYield();
  }

  private void RemoveHead() => LogEntries.RemoveAt(0);

  public async void HandleSelectFileClicked()
  {
    Services.Log.Info("Select file clicked");

    try
    {
      // On iOS, the file picker must run on the main thread
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

      var reader = new Reader();
      if (!reader.TryGetDecoder(file.Name, ms, out FitFile fit, out var decoder))
      {
        return;
      }

      long lastPosition = 0;
      long resolution = 5 * 1024; // report progress every 5 kB

      // Instead of reading all FIT messages at once,
      // Read just a few FIT messages at a time so that other tasks can run on the main thread e.g. in WASM
      Progress = 0;
      while (await reader.ReadOneAsync(ms, decoder, 100))
      {
        if (ms.Position - resolution > lastPosition)
        {
          continue;
        }

        double progress = (double)ms.Position / ms.Length * 100;
        Progress = progress;
        await TaskHelp.MaybeYield();
        lastPosition = ms.Position;
      }

      Progress = 100;
      await Log($"Done reading FIT file");

      FitFile = fit;
    }
    catch (Exception e)
    {
      Services.Log.Info($"{e}");
    }
  }

  public async void HandleDownloadFileClicked()
  {
    Services.Log.Info("Download file clicked...");

    try
    {
      if (lastFile_ == null)
      {
        await Log("Cannot download file; none has been uploaded");
        return;
      }

      string name = Path.GetFileNameWithoutExtension(lastFile_.Name);
      string extension = Path.GetExtension(lastFile_.Name);
      // On macOS, the file save dialog must run on the main thread
      await storage_.SaveAsync(new Models.File($"{name}_edit.{extension}", lastFile_.Bytes));
    }
    catch (Exception e)
    {
      Services.Log.Info($"{e}");
    }
  }

  private void Show(FitFile? fit)
  {
    if (fit == null) { return; }

    Plot.Show(fit);
    Laps.FitFile = fit;
    Records.Show(fit);
    Map.Show(fit);
    SliderMax = fit.Records.Count - 1;
  }

}
