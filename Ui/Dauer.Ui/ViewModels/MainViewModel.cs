using System.Reactive.Linq;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Windowing;
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
    new DesignLogViewModel(),
    new NullWebAuthenticator()
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
  public IWebAuthenticator Authenticator { get; }

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
    ILogViewModel log,
    IWebAuthenticator authenticator
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
    Authenticator = authenticator;

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

  public void HandleLoginClicked()
  {
    Dauer.Model.Log.Info($"{nameof(HandleLoginClicked)}");
    Dauer.Model.Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.AuthenticateAsync)}");

    Authenticator.AuthenticateAsync();
  }

  public void HandleLogoutClicked()
  {
    Dauer.Model.Log.Info($"{nameof(HandleLogoutClicked)}");
    Dauer.Model.Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.LogoutAsync)}");

    Authenticator.LogoutAsync();
  }
}