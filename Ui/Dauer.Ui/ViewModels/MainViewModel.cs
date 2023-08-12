using System.Reactive.Linq;
using Dauer.Model;
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

  [Reactive] public int SelectedTabIndex { get; set; }

  [Reactive] public string EmailOtp { get; set; } = "";
  [Reactive] public string AuthenticateMessage { get; set; } = "";

  private readonly IWindowAdapter window_;
  public IFitEditService FitEdit { get; set; }
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
    FitEdit = fitEdit;

    window_.Resized.Subscribe(tup =>
    {
      Log.Info($"Window resized to {tup.Item1} {tup.Item2}");
    });

    FitEdit.ObservableForProperty(x => x.IsAuthenticatedWithGarmin)
      .Subscribe(_ =>
      {
        AuthenticateMessage = FitEdit.IsAuthenticatedWithGarmin
          ? "Successfully connected to Garmin!" 
          : "Disconnected from Garmin";
      });
  }

  public void HandleLoginClicked()
  {
    Log.Info($"{nameof(HandleLoginClicked)}");

    if (!EmailValidator.IsValid(FitEdit.Username)) 
    {
      AuthenticateMessage = "Please enter a valid email address.";
      return; 
    }

    _ = Task.Run(async () => await FitEdit.AuthenticateAsync());
    AuthenticateMessage = "Please check your email"; 
  }

  public void HandleLogoutClicked()
  {
    Log.Info($"{nameof(HandleLogoutClicked)}");
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.LogoutAsync();
      AuthenticateMessage = ok ? "Signed out" : "There was a problem signing out";
    });
  }

  public void HandleGarminAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminAuthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      await FitEdit.AuthorizeGarminAsync();
      AuthenticateMessage = FitEdit.IsAuthenticatingWithGarmin
        ? "We've opened a link to Garmin" 
        : "There was a problem connecting to Garmin";
    });
  }

  public void HandleGarminDeauthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminDeauthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.DeauthorizeGarminAsync();
      AuthenticateMessage = ok ? "Disconnected from Garmin" : "There was a problem disconnecting from Garmin";
    });
  }

  public void HandleVerifyEmailClicked()
  {
    Log.Info($"{nameof(HandleVerifyEmailClicked)}");
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.VerifyEmailAsync(EmailOtp);
      AuthenticateMessage = ok ? "Sign in complete!" : "There was a problem verifying your code";
      // TODO stop loopback listener
    });
  }
}
