using System.Reactive.Linq;
using System.Reflection;
using Dauer.Model;
using Dauer.Model.Data;
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
    new FileService(new NullDatabaseAdapter()),
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

public class Titlebar : ReactiveObject
{
  public static Titlebar Instance { get; } = new();
  [Reactive] public string? Message { get; set; }
}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  public IPlotViewModel Plot { get; }
  public ILapViewModel Laps { get; }
  public IRecordViewModel Records { get; }
  public IMapViewModel Map { get; }
  public IFileViewModel File { get; }
  public ILogViewModel LogVm { get; }
  public IFitEditService FitEdit { get; set; }

  private string? AppTitle_ => $"FitEdit | Training Data Editor | Version {Version} {Titlebar.Instance.Message}";
  [Reactive] public string? AppTitle { get; set; }

  public string? Version { get; set; }

  [Reactive] public int SelectedTabIndex { get; set; }

  [Reactive] public string Otp { get; set; } = "";
  [Reactive] public string Message { get; set; } = "Please enter an email address and click Sign In";

  private CancellationTokenSource authCancelCts_ = new();
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

    GetVersion();

    Titlebar.Instance.ObservableForProperty(x => x.Message).Subscribe(_ => AppTitle = AppTitle_);
    window_.Resized.Subscribe(tup => Log.Info($"Window resized to {tup.Item1} {tup.Item2}"));

    FitEdit.ObservableForProperty(x => x.IsAuthenticatedWithGarmin)
      .Subscribe(_ =>
      {
        Message = FitEdit.IsAuthenticatedWithGarmin
          ? "Successfully connected to Garmin!"
          : "Disconnected from Garmin";
      });

    FitEdit.ObservableForProperty(x => x.IsAuthenticated)
      .Subscribe(_ =>
      {
        Message = FitEdit.IsAuthenticated
          ? "Sign in complete!"
          : "Signed out";
      });
  }

  /// <summary>
  /// Get the assembly version that is displayed in the titlebar and update the titlebar with it
  /// </summary>
  private void GetVersion()
  {
    var assembly = Assembly.GetAssembly(typeof(CompositionRoot));
    var attr = assembly?.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
    Version = attr?.InformationalVersion ?? "Unknown Version";
    AppTitle = AppTitle_;
  }

  public void HandleLoginClicked()
  {
    Log.Info($"{nameof(HandleLoginClicked)}");
    Otp = "";
    string? username = FitEdit.Username?.Trim();

    if (EmailValidator.IsValid(username))
    {
      Message = "We sent you an email. " +
        "\nIf you're a new user, it has a link. " +
        "\nIf you're a returning user, it has a code and a link. " +
        "\nEnter the code or open the link on this device within 5 minutes.";
    } else if (PhoneValidator.IsValid(username))
    {
      Message = "We sent you a text message with a code. " +
        "\nEnter the code within 5 minutes.";
    }
    else
    {
      Message = "Please enter a valid email address or phone number.";
      return;
    }

    // Cancel any existing authentication
    authCancelCts_.Cancel();
    authCancelCts_ = new();

    _ = Task.Run(async () => await FitEdit.AuthenticateAsync(authCancelCts_.Token));
  }

  public void HandleLogoutClicked()
  {
    Log.Info($"{nameof(HandleLogoutClicked)}");
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.LogoutAsync();
      Message = ok ? "Signed out" : "There was a problem signing out";
    });
  }

  public void HandleGarminAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminAuthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      await FitEdit.AuthorizeGarminAsync();
      Message = FitEdit.IsAuthenticatingWithGarmin
        ? "Check your web browser. We've opened a page to Garmin" 
        : "There was a problem connecting to Garmin";
    });
  }

  public void HandleGarminDeauthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminDeauthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.DeauthorizeGarminAsync();
      Message = ok ? "Disconnected from Garmin" : "There was a problem disconnecting from Garmin";
    });
  }

  public void HandleVerifyEmailClicked()
  {
    Log.Info($"{nameof(HandleVerifyEmailClicked)}");
    _ = Task.Run(async () =>
    {
      // The user can verify either by typing in the token or by clicking the link in the email.
      // We're here because they typed in the code, so we need to stop listening for the link to be clicked,
      // if it hasn't already timed out after 5 minutes.
      authCancelCts_.Cancel();
      authCancelCts_ = new();

      bool ok = await FitEdit.VerifyOtpAsync(Otp.Trim());
      Otp = "";
      Message = ok ? "Code is valid" : "There was a problem verifying the code";
    });
  }
}
