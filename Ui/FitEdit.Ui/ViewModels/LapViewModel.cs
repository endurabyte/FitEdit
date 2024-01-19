using ReactiveUI;
using System.Collections.ObjectModel;
using FitEdit.Data.Fit;
using FitEdit.Ui.Model;
using ReactiveUI.Fody.Helpers;
using FitEdit.Model;
using DynamicData.Binding;
using FitEdit.Ui.Extensions;
using Units;
using Avalonia.Threading;
using FitEdit.Data;
using FitEdit.Model.Workouts;

namespace FitEdit.Ui.ViewModels;

public interface ILapViewModel
{
  ObservableCollection<Lap> Laps { get; }
}

public class DesignLapViewModel : LapViewModel
{
  public DesignLapViewModel() : base(new NullFileService())
  {
    var now = DateTime.Now;
    Laps.Add(new Lap 
    { 
      Start = now, 
      End = now + TimeSpan.FromSeconds(60), 
      Speed = new(3.12345, Unit.MetersPerSecond),
      Distance = new(123.2, Unit.Meter),
    });
    Laps.Add(new Lap 
    { 
      Start = now + TimeSpan.FromSeconds(60), 
      End = now + TimeSpan.FromSeconds(120), 
      Speed = new(3.5, Unit.MetersPerSecond),
      Distance = new(1.23, Unit.Mile),
    });
    Laps.Add(new Lap
    {
      Start = now + TimeSpan.FromSeconds(120),
      End = now + TimeSpan.FromSeconds(180),
      Speed = new(2.7, Unit.MetersPerSecond),
      Distance = new(2.34, Unit.Kilometer),
    });
  }
}

public class ReactiveThing<T> : ReactiveObject
{
  [Reactive] public T? Value { get; set; }

  public ReactiveThing() { }
  public ReactiveThing(T value)
  {
    Value = value;
  }

  public override string ToString() => $"{Value}";
}

public class LapViewModel : ViewModelBase, ILapViewModel
{
  [Reactive] public ObservableCollection<Lap> Laps { get; set; } = new();

  [Reactive] public bool ApplyingLapSpeeds { get; set; }
  [Reactive] public double Progress { get; set; }
  [Reactive] public int SelectedIndex { get; set; }

  private readonly Dictionary<int, Speed> editedLaps_ = new();

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
        Speed = new Speed(lap.GetEnhancedAvgSpeed() ?? 0, Unit.MetersPerSecond).Convert(Unit.MilesPerHour),
        Distance = new Distance(lap.GetTotalDistance() ?? 0, Unit.Meter).Convert(Unit.Mile),

        // Find first record of lap by timestamp
        RecordFirstIndex = fit.Records.FindIndex(0, fit.Records.Count, r => r.InstantOfTime() == lap.Start()),
        RecordLastIndex = fit.Records.FindIndex(0, fit.Records.Count, r => r.InstantOfTime() == lap.End()),
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

  private void SubscribeToLapSpeedValueChanges(int i, Speed speed)
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

  public void HandleSetLapSpeedsClicked() => _ = Task.Run(async () => await ApplyLapSpeeds(editedLaps_));
  public void HandleSetLapDistancesClicked() => _ = Task.Run(async () => await SetLapDistances(Laps.Select(l => l.Distance!).ToList()));

  public void HandleAddLapClicked() => Laps.Add(new Lap());
  public void HandleRemoveLapClicked()
  {
    if (SelectedIndex < 0 || SelectedIndex >= Laps.Count) { return; }
    Laps.RemoveAt(SelectedIndex);
  }

  private async Task ApplyLapSpeeds(Dictionary<int, Speed> speeds)
  {
    if (uneditedLaps_ == null) { return; }

    var fit = new FitFile(fileService_.MainFile?.FitFile);

    if (fit == null)
    {
      Log.Error("No file loaded");
      return;
    }

    Log.Info("Applying new lap speeds");

    ApplyingLapSpeeds = true;
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

    await fileService_.CreateAsync(fit);
    ApplyingLapSpeeds = false;
  }

  private async Task SetLapDistances(List<Distance> distances)
  {
    if (fileService_.MainFile?.FitFile is null) { return; }

    var fit = new FitFile(fileService_.MainFile?.FitFile);

    if (fit == null)
    {
      Log.Error("No file loaded");
      return;
    }

    Log.Info($"Setting lap distances");

    var laps = new List<Lap>();

    int lapi = 0;
    int recordi = 0;

    double cumulativeDistance = distances[lapi].Meters();

    foreach (var record in fit.Records)
    {
      double dist = record.GetDistance() ?? 0;
      if (dist < cumulativeDistance)
      {
        recordi++;
        continue;
      }

      laps.Add(new Lap
      {
        RecordFirstIndex = lapi == 0 ? 0 : laps[lapi - 1].RecordLastIndex + 1,
        RecordLastIndex = recordi
      });

      lapi++;
      recordi++;

      if (lapi == distances.Count)
      {
        break;
      }

      cumulativeDistance += distances[lapi].Meters();
    }

    double remainder = recordi < 0 || recordi >= fit.Records.Count 
      ? 0 
      : (fit.Records.Last().GetDistance() ?? 0) - (fit.Records[recordi].GetDistance() ?? 0);

    if (remainder > 0)
    {
      laps.Add(new Lap
      {
        RecordFirstIndex = lapi == 0 ? 0 : laps[lapi - 1].RecordLastIndex + 1,
        RecordLastIndex = fit.Records.Count - 1,
      });
    }

    fit.RemoveAll<Dynastream.Fit.LapMesg>();
    fit.MessagesByDefinition.Remove(Dynastream.Fit.MesgNum.Lap);

    foreach (var lap in laps)
    {
      var r1 = fit.Records[lap.RecordFirstIndex];
      var r2 = fit.Records[lap.RecordLastIndex];

      var lapMesg = new Dynastream.Fit.LapMesg();
      float? dist = r2.GetDistance() - r1.GetDistance();
      lapMesg.SetTotalDistance(dist);
      lapMesg.SetStartTime(r1.GetTimestamp());
      lapMesg.SetTimestamp(r2.GetTimestamp());

      lapMesg.SetStartPositionLat(r1.GetPositionLat());
      lapMesg.SetEndPositionLat(r1.GetPositionLat());
      lapMesg.SetStartPositionLong(r2.GetPositionLong());
      lapMesg.SetEndPositionLong(r2.GetPositionLong());

      var seconds = (float)(r2.InstantOfTime() - r1.InstantOfTime()).TotalSeconds;
      lapMesg.SetTotalElapsedTime(seconds);
      lapMesg.SetTotalTimerTime(seconds);

      lapMesg.SetSport(fit.Get<Dynastream.Fit.SportMesg>().FirstOrDefault()?.GetSport() ?? Dynastream.Fit.Sport.Generic);
      lapMesg.SetSubSport(fit.Get<Dynastream.Fit.SportMesg>().FirstOrDefault()?.GetSubSport() ?? Dynastream.Fit.SubSport.Generic);
      lapMesg.SetLapTrigger(Dynastream.Fit.LapTrigger.Distance);

      lapMesg.SetEnhancedAvgSpeed(dist / seconds);

      fit.Add(lapMesg);
    }

    fit.ForwardfillEvents();
    await fileService_.CreateAsync(fit);
  }
}