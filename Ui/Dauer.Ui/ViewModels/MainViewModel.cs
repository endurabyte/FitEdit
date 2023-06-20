using System.Reactive.Linq;
using Dauer.Services;
using Dauer.Ui.Infra.Adapters.Windowing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{

}

public class DesignMainViewModel : MainViewModel
{
  public DesignMainViewModel() : base(
    new FileService(),
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
  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }
  public IFileViewModel File { get; }
  public ILogViewModel Log { get; }

  [Reactive] public string Text { get; set; } = "Welcome to FitEdit. Please load a FIT file.";
  [Reactive] public int SliderValue { get; set; }
  [Reactive] public int SliderMax { get; set; }

  private readonly IWindowAdapter window_;
  private readonly IFileService fileService_;

  public MainViewModel(
    IFileService fileService,
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
    fileService_ = fileService;
    Plot = plot;
    Laps = laps;
    Records = records;
    Map = map;
    File = file;
    Log = log;

    window_.Resized.Subscribe(tup =>
    {
      Dauer.Model.Log.Info($"Window resized to {tup.Item1} {tup.Item2}");
    });

    this.ObservableForProperty(x => x.SliderValue).Subscribe(property =>
    {
      fileService.SelectedIndex = property.Value;
    });

    fileService.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      if (property.Value == null) { return; }
      SliderMax = property.Value.Records.Count - 1;
    });

    fileService.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      SliderValue = property.Value;
    });
  }
}