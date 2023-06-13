using ReactiveUI;
using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Model.Units;
using Dauer.Ui.Model;
using ReactiveUI.Fody.Helpers;
using Dauer.Model;
using System.Text;
using DynamicData.Binding;
using Dauer.Ui.Extensions;

namespace Dauer.Ui.ViewModels;

public interface ILapViewModel
{
  ObservableCollection<Lap> Laps { get; }
  FitFile? FitFile { get; set; }
}

public class DesignLapViewModel : LapViewModel
{
  public DesignLapViewModel() 
  {
    var now = DateTime.Now;
    Laps.Add(new Lap { Start = now, End = now + TimeSpan.FromSeconds(60), Speed = new(3.12345, SpeedUnit.MetersPerSecond) });
    Laps.Add(new Lap { Start = now + TimeSpan.FromSeconds(60), End = now + TimeSpan.FromSeconds(120), Speed = new(3.5, SpeedUnit.MetersPerSecond) });
    Laps.Add(new Lap { Start = now + TimeSpan.FromSeconds(120), End = now + TimeSpan.FromSeconds(180), Speed = new(2.7, SpeedUnit.MetersPerSecond) });
  }
}

public class LapViewModel : ViewModelBase, ILapViewModel
{
  public ObservableCollection<Lap> Laps { get; set; } = new();

  private readonly Dictionary<int, Dauer.Model.Workouts.Speed> editedLaps_ = new();

  private readonly List<IDisposable> subscriptions_ = new();
  private FitFile? uneditedFitFile_;
  [Reactive] public FitFile? FitFile { get; set; }
  [Reactive] public double Progress { get; set; }

  public LapViewModel()
  {
    this.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      if (property.Value == null)
      {
        return;
      }

      uneditedFitFile_ = new FitFile(property.Value).ForwardfillEvents();
      Show(property.Value);
    });
  }

  public void Show(FitFile fit)
  {
    Laps.Clear();
    editedLaps_.Clear();

    // Unsubscribe from previous laps
    foreach (var sub in subscriptions_) { sub.Dispose(); }

    // Show laps in the ListBox
    foreach (var lap in fit.Laps)
    {
      var rl = new Lap
      {
        Start = lap.Start(),
        End = lap.End(),
        Speed = new(lap.GetEnhancedAvgSpeed() ?? 0, SpeedUnit.MetersPerSecond)
      };
      Laps.Add(rl);
    }

    SubscribeToLapChanges(Laps);
  }

  private void SubscribeToLapChanges(IEnumerable<Lap> laps)
  {
    var pairs = laps.Select((lap, i) => new { lap.Speed, i }).ToList();

    foreach (var pair in pairs)
    {
      var speed = pair.Speed!;
      var sub = speed.WhenPropertyChanged(x => x.Value).Subscribe(property =>
      {
        // Ignore small changes
        double originalSpeed = uneditedFitFile_?.Laps[pair.i].GetEnhancedAvgSpeed() ?? 0;

        if (Math.Abs(originalSpeed - property.Value) < 1e-5)
        {
          return;
        }

        editedLaps_[pair.i] = speed;
      });

      subscriptions_.Add(sub);
    }
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

    FitFile = fit;
  }
}
