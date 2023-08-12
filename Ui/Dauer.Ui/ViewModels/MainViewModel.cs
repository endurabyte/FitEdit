using System.Diagnostics;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Text.Json;
using System.Web;
using Dauer.Model;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Windowing;
using Dauer.Ui.Supabase;
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
    new NullWebAuthenticator(),
    new NullFitEditService()
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
  public ILogViewModel LogVm { get; }
  public IWebAuthenticator Authenticator { get; }

  [Reactive] public int SelectedTabIndex { get; set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }

  private readonly IWindowAdapter window_;
  private readonly IFitEditService fitEdit_;
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
    IWebAuthenticator authenticator,
    IFitEditService fitEdit
  )
  {
    window_ = window;
    fileService_ = fileService;
    Plot = plot;
    Laps = laps;
    Records = records;
    Map = map;
    File = file;
    LogVm = log;
    Authenticator = authenticator;
    fitEdit_ = fitEdit;
    window_.Resized.Subscribe(tup =>
    {
      Log.Info($"Window resized to {tup.Item1} {tup.Item2}");
    });

    fitEdit_.ObservableForProperty(x => x.IsAuthenticatedWithGarmin).Subscribe(_ =>
    {
      IsAuthenticatedWithGarmin = fitEdit_.IsAuthenticatedWithGarmin;
    });
  }

  public void HandleLoginClicked()
  {
    Log.Info($"{nameof(HandleLoginClicked)}");
    Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.AuthenticateAsync)}");

    _ = Task.Run(() => Authenticator.AuthenticateAsync());
  }

  public void HandleLogoutClicked()
  {
    Log.Info($"{nameof(HandleLogoutClicked)}");
    Log.Info($"Starting {Authenticator.GetType()}.{nameof(IWebAuthenticator.LogoutAsync)}");

    _ = Task.Run(() => Authenticator.LogoutAsync());
  }

  public void HandleGarminAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminAuthorizeClicked)}");

    _ = Task.Run(AuthorizeGarminAsync);
  }

  public void HandleGarminDeauthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminDeauthorizeClicked)}");

    _ = Task.Run(DeauthorizeGarminAsync);
  }

  private async Task AuthorizeGarminAsync()
  {
    await fitEdit_.AuthorizeGarminAsync(Authenticator.Username);
  }

  private async Task DeauthorizeGarminAsync()
  {
    await fitEdit_.DeauthorizeGarminAsync(Authenticator.Username);
  }
}
