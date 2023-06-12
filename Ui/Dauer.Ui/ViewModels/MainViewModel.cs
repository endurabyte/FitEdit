using System.Reactive.Linq;
using Dauer.Data.Fit;
using Dauer.Services;
using Dauer.Ui.Adapters.Windowing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{

}

public class DesignMainViewModel : MainViewModel
{
  public DesignMainViewModel() : base(
    new NullFitService(),
    new NullWindowAdapter(),
    new DesignPlotViewModel(),
    new DesignLapViewModel(),
    new DesignRecordViewModel(),
    new DesignMapViewModel(),
    new DesignFileViewModel(),
    new DesignLogViewModel()
  )
  { 
  }
}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  private readonly IWindowAdapter window_;
  private readonly IFitService fit_;

  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }
  public IFileViewModel File { get; }
  public ILogViewModel Log { get; }

  [Reactive] public string Text { get; set; } = "Welcome to FitEdit. Please load a FIT file.";
  [Reactive] public int SliderValue { get; set; }
  [Reactive] public int SliderMax { get; set; }

  public MainViewModel(
    IFitService fit,
    IWindowAdapter window,
    IPlotViewModel plot,
    ILapViewModel laps,
    IRecordViewModel records,
    IMapViewModel map,
    IFileViewModel file,
    ILogViewModel log
  )
  {
    window_ = window;
    fit_ = fit;
    Plot = plot;
    Laps = laps;
    Records = records;
    Map = map;
    File = file;
    Log = log;

    window_.Resized.Subscribe(tup =>
    {
      Model.Log.Info($"Window resized to {tup.Item1} {tup.Item2}");
    });

    // When the records list selection changes, show it in the plot and on the map and update the slider
    records.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      plot.SelectedIndex = property.Value;
      Map.SelectedIndex = property.Value;
      SliderValue = property.Value;
    });

    // When plot selected data point changes, show it on the map and in the records list and update the slider
    plot.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      records.SelectedIndex = property.Value;
      Map.SelectedIndex = property.Value;
      SliderValue = property.Value;
    });

    // When the slider moves, update the plot, which also updates the map and records list.
    this.ObservableForProperty(x => x.SliderValue)
     .Subscribe(property =>
    {
      plot.SelectedIndex = property.Value;
    });

    // When a fit file is edited in the laps view, show it in the plot, records list, and map
    laps.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      File.FitFile = property.Value;
    });

    // When a fit file is loaded, show it in the plot, recods list, and map
    File.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      Show(File.FitFile);
    });
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
