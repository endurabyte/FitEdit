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

  [Reactive] public int SelectedTabIndex { get; set; }

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
  }

  public void HandleLoginClicked()
  {
    Dauer.Model.Log.Info($"{nameof(HandleLoginClicked)}");
    Dauer.Model.Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.AuthenticateAsync)}");

    _ = Task.Run(() => Authenticator.AuthenticateAsync());
  }

  public void HandleLogoutClicked()
  {
    Dauer.Model.Log.Info($"{nameof(HandleLogoutClicked)}");
    Dauer.Model.Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.LogoutAsync)}");

    _ = Task.Run(() => Authenticator.LogoutAsync());
  }
}