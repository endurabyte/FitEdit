using ReactiveUI;
using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Ui.Model;
using ReactiveUI.Fody.Helpers;
using Dauer.Model;
using DynamicData.Binding;
using Dauer.Ui.Extensions;
using Units;
using Avalonia.Threading;
using Dauer.Model.Data;
using Dauer.Data;

namespace Dauer.Ui.ViewModels;

public interface ILapViewModel
{
  ObservableCollection<Lap> Laps { get; }
}

public class DesignLapViewModel : LapViewModel
{
  public DesignLapViewModel() : base(new NullFileService())
  {
    var now = DateTime.Now;
    Laps.Add(new Lap { Start = now, End = now + TimeSpan.FromSeconds(60), Speed = new(3.12345, Unit.MetersPerSecond) });
    Laps.Add(new Lap { Start = now + TimeSpan.FromSeconds(60), End = now + TimeSpan.FromSeconds(120), Speed = new(3.5, Unit.MetersPerSecond) });
    Laps.Add(new Lap { Start = now + TimeSpan.FromSeconds(120), End = now + TimeSpan.FromSeconds(180), Speed = new(2.7, Unit.MetersPerSecond) });
  }
}

public class LapViewModel : ViewModelBase, ILapViewModel
{
  [Reactive] public ObservableCollection<Lap> Laps { get; set; } = new();
  [Reactive] public double Progress { get; set; }
  [Reactive] public int SelectedIndex { get; set; }

  private readonly Dictionary<int, Dauer.Model.Workouts.Speed> editedLaps_ = new();

  private readonly List<IDisposable> subscriptions_ = new();
  private List<Lap>? uneditedLaps_;

  private readonly IFileService fileService_;

  public LapViewModel
  (
    IFileService fileService
  )
  {
    fileService_ = fileService;

    fileService.ObservableForProperty(x => x.MainFile).Subscribe(property => HandleMainFileChanged(fileService.MainFile));
    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property => HandleSelectedIndexChanged(property.Value));
  }

  private void HandleMainFileChanged(UiFile? file)
  {
    if (file?.FitFile == null) { return; }
    Show(file.FitFile);
  }

  private void HandleSelectedIndexChanged(int index)
  {
    if (index < 0 || index >= Laps.Count) { return; }
    Lap lap = Laps[index];

    UiFile? file = fileService_.MainFile;
    if (file == null) { return; }

    file.SelectedIndex = lap.RecordFirstIndex;
    file.SelectionCount = lap.RecordLastIndex - lap.RecordFirstIndex;
  }

  private void Show(FitFile fit)
  {
    ClearLaps();

    // Unsubscribe from previous laps
    foreach (var sub in subscriptions_) { sub.Dispose(); }

    // Show laps in the ListBox
    foreach (var lap in fit.Laps)
    {
      var rl = new Lap
      {
        Start = lap.Start().ToLocalTime(),
        End = lap.End().ToLocalTime(),
        Speed = new Dauer.Model.Workouts.Speed(lap.GetEnhancedAvgSpeed() ?? 0, Unit.MetersPerSecond).Convert(Unit.MilesPerHour),
        Distance = new Dauer.Model.Workouts.Distance(lap.GetTotalDistance() ?? 0, Unit.Meter).Convert(Unit.Mile),

        // Find first record of lap by timestamp
        RecordFirstIndex = fit.Records.FindIndex(0, fit.Records.Count, r => r.Start() == lap.Start()),
        RecordLastIndex = fit.Records.FindIndex(0, fit.Records.Count, r => r.Start() == lap.End()),
      };

      if (rl.RecordFirstIndex < 0)
      {
        rl.RecordFirstIndex = 0;
      }

      if (rl.RecordLastIndex < 0)
      {
        rl.RecordLastIndex = fit.Records.Count - 1;
      }
      Laps.Add(rl);
    }
    uneditedLaps_ = Laps.Select(l => new Lap(l)).ToList();

    SubscribeToLapChanges(Laps);
  }

  private void ClearLaps()
  {
    //Laps.Clear(); // Results in duplicated list
    while (Laps.Count != 0)
    {
      Laps.RemoveAt(0);
    }
    editedLaps_.Clear();
  }

  private void SubscribeToLapChanges(IEnumerable<Lap> laps)
  {
    var lapsByIndex = laps.Select((lap, i) => new { lap, i }).ToList();

    foreach (var pair in lapsByIndex)
    {
      SubscribeToLapSpeedChanges(pair.i, pair.lap);
    }
  }

  private void SubscribeToLapSpeedChanges(int i, Lap lap)
  {
    var sub = lap.WhenPropertyChanged(x => x.Speed).Subscribe(property =>
    {
      SubscribeToLapSpeedValueChanges(i, lap.Speed!);
    });

    subscriptions_.Add(sub);
  }

  private void SubscribeToLapSpeedValueChanges(int i, Dauer.Model.Workouts.Speed speed)
  {
    var sub = speed.WhenPropertyChanged(x => x.Value).Subscribe(property =>
    {
      var metric = speed.Convert(Unit.MetersPerSecond);

      // Ignore small changes
      double originalSpeed = uneditedLaps_?[i].Speed?.Convert(Unit.MetersPerSecond)?.Value ?? 0;

      if (Math.Abs(originalSpeed - metric.Value) < 1e-5)
      {
        return;
      }

      editedLaps_[i] = speed;
    });

    subscriptions_.Add(sub);
  }

  public void HandleApplyClicked() => _ = Task.Run(async () => await ApplyLapSpeeds(editedLaps_));

  public async Task ApplyLapSpeeds(Dictionary<int, Dauer.Model.Workouts.Speed> speeds)
  {
    if (uneditedLaps_ == null) { return; }

    FitFile? fit = fileService_.MainFile?.FitFile;

    if (fit == null)
    {
      Log.Error("No file loaded");
      return;
    }

    Log.Info("Applying new lap speeds");

    fit.ApplySpeeds(speeds, 100, async (i, count) =>
    {
      Progress = 0.5 * i / count * 100;
      Log.Info($"Applying speeds: {Progress:##.##}% ({i}/{count})");
      await TaskUtil.MaybeYield();
    });
    Progress = 50;

    Log.Info("Backfilling: ");

    fit.BackfillEvents(100, async (i, count) =>
    {
      double progress = 50 + 0.5 * i / count * 100;
      Progress = progress;
      Log.Info($"Backfilling: {progress:##.##}% ({i}/{count})");
      await TaskUtil.MaybeYield();
    });
    Progress = 100;

    Log.Info("Backfilling: 100%");

    UiFile? file = fileService_.MainFile;

    // Trigger property change
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
      fileService_.MainFile = null;
      fileService_.MainFile = file;
    });
  }
}