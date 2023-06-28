using System.Reactive.Linq;
using Dauer.Ui.Infra.Adapters.Windowing;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{
  IMapViewModel Map { get; }
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

  [Reactive] public int SliderValue { get; set; }
  [Reactive] public int SliderMax { get; set; }
  [Reactive] public int SelectedTabIndex { get; set; }

  private readonly IWindowAdapter window_;
  private readonly IFileService fileService_;
  private IDisposable? recordIndexSub_;

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
      SelectedFile? file = fileService.MainFile;
      if (file == null) { return; }

      file.SelectedIndex = property.Value;
    });

    fileService.ObservableForProperty(x => x.MainFile).Subscribe(property =>
    {
      SelectedFile? file = fileService.MainFile;
      if (file == null) { return; }

      recordIndexSub_?.Dispose();
      recordIndexSub_ = file.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
      {
        SliderValue = property.Value;
      });

      if (file.FitFile == null) { return; }
      SliderMax = file.FitFile.Records.Count - 1;
      //SelectedTabIndex = 1; // Laps
    });
  }
}