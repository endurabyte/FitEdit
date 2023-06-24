using ReactiveUI;
using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Ui.Model;
using ReactiveUI.Fody.Helpers;
using Dauer.Model;
using DynamicData.Binding;
using Dauer.Ui.Extensions;
using Units;

namespace Dauer.Ui.ViewModels;

public interface ILapViewModel
{
  ObservableCollection<Lap> Laps { get; }
}

public class DesignLapViewModel : LapViewModel
{
  public DesignLapViewModel() : base(new FileService())
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
  private FitFile? uneditedFitFile_;

  private readonly IFileService fileService_;

  public LapViewModel(
    IFileService fileService
  )
  {
    fileService_ = fileService;

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      Lap lap = Laps[property.Value];

      fileService_.SelectedIndex = lap.RecordIndex;
    });

    fileService.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      if (property.Value == null) { return; }
      uneditedFitFile_ = new FitFile(property.Value).ForwardfillEvents();
      Show(property.Value);
    });
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
        RecordIndex = fit.Records.FindIndex(0, fit.Records.Count, r => r.Start() == lap.Start()),
      };
      Laps.Add(rl);
    }

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
      // Ignore small changes
      double originalSpeed = uneditedFitFile_?.Laps[i].GetEnhancedAvgSpeed() ?? 0;

      if (Math.Abs(originalSpeed - property.Value) < 1e-5)
      {
        return;
      }

      editedLaps_[i] = speed;
    });

    subscriptions_.Add(sub);
  }

  public void HandleApplyClicked() => _ = Task.Run(() => ApplyLapSpeeds(editedLaps_));

  public void ApplyLapSpeeds(Dictionary<int, Dauer.Model.Workouts.Speed> speeds)
  {
    if (uneditedFitFile_ == null) { return; }

    FitFile? fit = uneditedFitFile_;

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
      //Progress = progress;
      Log.Info($"Backfilling: {progress:##.##}% ({i}/{count})");
      await TaskUtil.MaybeYield();
    });
    Progress = 100;

    Log.Info("Backfilling: 100%");

    fileService_.FitFile = fit;
  }
}