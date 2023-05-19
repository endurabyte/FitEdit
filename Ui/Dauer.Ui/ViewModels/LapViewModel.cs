using ReactiveUI;
using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Model.Units;
using Dauer.Ui.Models;

namespace Dauer.Ui.ViewModels;

public interface ILapViewModel
{
  ObservableCollection<Lap> Laps { get; }
  void Show(FitFile fit);
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

  public void Show(FitFile fit)
  {
    Laps.Clear();

    foreach (var lap in fit.Laps)
    {
      var rl = new Lap { Start = lap.Start(), End = lap.End(), Speed = new(lap.GetEnhancedAvgSpeed() ?? 0, SpeedUnit.MetersPerSecond) };
      Laps.Add(rl);
    }
  }
}
