using System.Reactive.Linq;
using FitEdit.Model;
using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using FitEdit.Model.GarminConnect;
using FitEdit.Model.Strava;
using FitEdit.Model.Validators;
using FitEdit.Model.Web;
using FitEdit.Services;
using FitEdit.Ui.Infra;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.ViewModels;

public interface ISettingsViewModel
{

}

public class DesignSettingsViewModel : SettingsViewModel
{
  public DesignSettingsViewModel() : base(
    new DesignNotifyService(),
    new NullDatabaseAdapter(),
    new NullFitEditService(),
    new NullGarminConnectClient(),
    new NullStravaClient(),
    new NullEmailValidator(),
    new NullPhoneValidator(),
    new NullBrowser()
  )
  {
  }

  public override void HandleManagePaymentsClicked()
  {
    if (FitEdit is NullFitEditService fake)
    {
      fake.IsActive = false;
    }
  }

  public override void HandleSignUpClicked()
  {
    if (FitEdit is NullFitEditService fake)
    {
      fake.IsActive = true;
    }
  }

  public override async Task HandleGarminLoginClicked()
  {
     await Garmin.IsAuthenticatedAsync();
  }

  public override async Task HandleGarminLogoutClicked()
  {
    await Garmin.LogoutAsync();
  }

  public override async Task HandleStravaLoginClicked()
  {
    await Strava.AuthenticateAsync();
  }

  public override async Task HandleStravaLogoutClicked()
  {
    await Strava.LogoutAsync();
  }
}

public class SettingsViewModel : ViewModelBase, ISettingsViewModel
{
  public IFitEditService FitEdit { get; set; }
  public IGarminConnectClient Garmin { get; set; }
  public IStravaClient Strava { get; }

  [Reactive] public string Otp { get; set; } = "";

  [Reactive] public string? GarminSsoId { get; set; }
  [Reactive] public string? GarminSessionId { get; set; }

  [Reactive] public string? StravaUsername { get; set; }
  [Reactive] public string? StravaPassword { get; set; }

  public string PaymentPortalUrl => $"https://billing.stripe.com/p/login/5kA8Ap72G1oebZK9AA?prefilled_email={FitEdit.Username}";
  //public string PaymentPortalUrl => $"https://billing.stripe.com/p/login/test_6oE7vA7Yzfwg1eo144?prefilled_email={FitEdit.Username}";
  public string SignUpUrl => $"https://www.fitedit.io/pricing.html";

  private CancellationTokenSource authCancelCts_ = new();
  private readonly INotifyService notifier_;
  private readonly IDatabaseAdapter db_;
  private readonly IEmailValidator emailValidator_;
  private readonly IPhoneValidator phoneValidator_;
  private readonly IBrowser browser_;

  public SettingsViewModel(
    INotifyService notifier,
    IDatabaseAdapter db,
    IFitEditService fitEdit,
    IGarminConnectClient garmin,
    IStravaClient strava,
    IEmailValidator emailValidator,
    IPhoneValidator phoneValidator,
    IBrowser browser
  )
  {
    notifier_ = notifier;
    db_ = db;
    FitEdit = fitEdit;
    Garmin = garmin;
    Strava = strava;
    emailValidator_ = emailValidator;
    phoneValidator_ = phoneValidator;
    browser_ = browser;

    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ => await LoadSettings_());
    _ = Task.Run(LoadSettings_);

    FitEdit.ObservableForProperty(x => x.IsAuthenticatedWithGarmin)
      .Subscribe(_ =>
      {
        notifier_.NotifyUser(FitEdit.IsAuthenticatedWithGarmin
          ? "Successfully connected to Garmin!"
          : "Disconnected from Garmin", autoDismiss: true);
      });

    FitEdit.ObservableForProperty(x => x.IsAuthenticatedWithStrava)
      .Subscribe(_ =>
      {
        notifier_.NotifyUser(FitEdit.IsAuthenticatedWithStrava
          ? "Successfully connected to Strava!"
          : "Disconnected from Strava", autoDismiss: true);
      });

    FitEdit.ObservableForProperty(x => x.IsAuthenticated)
      .Subscribe(_ =>
      {
        notifier_.NotifyUser(FitEdit.IsAuthenticated
          ? "Sign in complete!"
          : "Signed out", autoDismiss: true);
      });

    FitEdit.ObservableForProperty(x => x.GarminCookies)
      .Subscribe(async _ =>
      {
        notifier_.NotifyUser("Received new Garmin cookies from the FitEdit browser extension.", autoDismiss: true);

        GarminSsoId = FitEdit.GarminCookies.FirstOrDefault(c => c.Name == "GARMIN-SSO-CUST-GUID")?.Value ?? null;
        GarminSessionId = FitEdit.GarminCookies.FirstOrDefault(c => c.Name == "SESSIONID")?.Value ?? null;
        Garmin.Config.SsoId = GarminSsoId;
        Garmin.Config.SessionId = GarminSessionId;

        garmin.Cookies = FitEdit.GarminCookies.ToDictionaryAllowDuplicateKeys(c => c.Name ?? "", c => new Cookie
        {
          Name = c.Name ?? "",
          Value = c.Value ?? "",
          Path = c.Path ?? "",
          Domain = c.Domain ?? ""
        });

        await LoginWithGarminAsync();
      });

    garmin.ObservableForProperty(x => x.Cookies).Subscribe(async _ =>
    {
      await UpdateSettingsAsync(settings => settings.GarminCookies = Garmin.Cookies);
    });

    strava.ObservableForProperty(x => x.Cookies).Subscribe(async _ =>
    {
      await UpdateSettingsAsync(settings => settings.StravaCookies = Strava.Cookies);
    });

    FitEdit.ObservableForProperty(x => x.LastSync).Subscribe(async _ =>
    {
      await UpdateSettingsAsync(settings => settings.LastSynced = FitEdit.LastSync);
    });
  }

  private async Task LoadSettings_()
  {
    if (!db_.Ready) { return; }

    AppSettings? settings = await db_.GetAppSettingsAsync().AnyContext() ?? new AppSettings();
    GarminSsoId = settings.GarminSsoId;
    GarminSessionId = settings.GarminSessionId;
    Garmin.Cookies = settings.GarminCookies;

    StravaUsername = settings.StravaUsername;
    StravaPassword = settings.StravaPassword;
    Strava.Cookies = settings.StravaCookies;

    await LoginWithStravaAsync();
  }

  public void HandleLoginClicked()
  {
    Log.Info($"{nameof(HandleLoginClicked)}");
    Otp = "";
    string? username = FitEdit.Username?.Trim();

    if (emailValidator_.IsValid(username))
    {
      notifier_.NotifyUser("We sent you an email. " +
        "If you're a new user, it has a link. " +
        "If you're a returning user, it has a code and a link. " +
        "Enter the code or open the link on this device within 5 minutes.", autoDismiss: true);
    } else if (phoneValidator_.IsValid(username))
    {
      notifier_.NotifyUser("We sent you a text message with a code. " +
        "\nEnter the code within 5 minutes.", autoDismiss: true);
    }
    else
    {
      notifier_.NotifyUser("Please enter a valid email address or phone number.", autoDismiss: true);
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
      notifier_.NotifyUser(ok ? "Signed out of FitEdit" : "There was a problem signing out of FitEdit", autoDismiss: true);
    });
  }

  public void HandleGarminAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminAuthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      await FitEdit.AuthorizeGarminAsync();
      notifier_.NotifyUser(FitEdit.IsAuthenticatingWithGarmin
        ? "Check your web browser. We've opened the Garmin authorize page" 
        : "There was a problem authorizing with Garmin", autoDismiss: true);
    });
  }

  public void HandleGarminDeauthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminDeauthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.DeauthorizeGarminAsync();
      notifier_.NotifyUser(ok ? "Deauthorized Garmin" : "There was a problem deauthorizing Garmin", autoDismiss: true);
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
      notifier_.NotifyUser(ok ? "Code is valid" : "There was a problem verifying the code", autoDismiss: true);
    });
  }

  public virtual void HandleManagePaymentsClicked()
  {
    _ = Task.Run(async () =>
    {
      await browser_.OpenAsync(PaymentPortalUrl);
      notifier_.NotifyUser("Check your web browser. We've opened a page to manage your payment.", autoDismiss: true);
    });
  }

  public virtual void HandleSignUpClicked()
  {
    _ = Task.Run(async () =>
    {
      await browser_.OpenAsync(SignUpUrl);
      notifier_.NotifyUser("Check your web browser. We've opened a page to sign up.", autoDismiss: true);
    });
  }

  public void HandleStravaAuthorizeClicked()
  {
    _ = Task.Run(async () =>
    {
      await FitEdit.AuthorizeStravaAsync();
      notifier_.NotifyUser(FitEdit.IsAuthenticatingWithStrava
        ? "Check your web browser. We've opened the Strava authorize page"
        : "There was a problem authorizing with Strava", autoDismiss: true);
    });
  }
  
  public void HandleStravaDeauthorizeClicked()
  {
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.DeauthorizeStravaAsync();
      notifier_.NotifyUser(ok ? "Deauthorized Strava" : "There was a problem deauthorizing Strava", autoDismiss: true);
    });
  }

  public virtual async Task HandleGarminLoginClicked() => await LoginWithGarminAsync();

  public async Task LoginWithGarminAsync()
  {
    bool signedIn = false;

    if (GarminSsoId == null || GarminSessionId == null) { return; }

    const int ntries = 5;
    foreach (int i in Enumerable.Range(0, ntries))
    {
      Garmin.Config.SsoId = GarminSsoId;
      Garmin.Config.SessionId = GarminSessionId;

      Garmin.Config.Username = null;
      Garmin.Config.Password = null;
      Garmin.Cookies = new();

      signedIn = await Garmin.IsAuthenticatedAsync();
      if (signedIn) { break; }

      await Task.Delay(2000);
    }

    if (!signedIn)
    {
      notifier_.NotifyUser("There was a problem signing in to Garmin", autoDismiss: true);
      return;
    }

    // Save login
    await UpdateSettingsAsync(settings =>
    {
      settings.GarminUsername = Garmin.Config.Username;
      settings.GarminPassword = Garmin.Config.Password;
      settings.GarminSsoId = Garmin.Config.SsoId;
      settings.GarminSessionId = Garmin.Config.SessionId;
      settings.GarminCookies = Garmin.Cookies;
    });
  }

  public virtual async Task HandleGarminLogoutClicked()
  {
    await Garmin.LogoutAsync();

    GarminSsoId = null;
    GarminSessionId = null;

    await UpdateSettingsAsync(settings =>
    {
      settings.GarminUsername = null;
      settings.GarminPassword = null;
      settings.GarminSsoId = null;
      settings.GarminSessionId = null;
      settings.GarminCookies = null;
    });

    notifier_.NotifyUser("Signed out of Garmin", autoDismiss: true);
  }

  public async Task HandleTermsClicked() => await browser_.OpenAsync("https://www.fitedit.io/support/user-agent-terms.html");
  public async Task HandleGarminLoginTellMeMoreClicked() => await browser_.OpenAsync("https://www.fitedit.io/support/garmin-signin.html");

  public virtual async Task HandleStravaLoginClicked() => await LoginWithStravaAsync();

  public async Task LoginWithStravaAsync()
  {
    bool signedIn = await Strava.IsAuthenticatedAsync();

    if (signedIn)
    {
      return;
    }

    if (StravaUsername == null || StravaPassword == null) { return; }

    Strava.Config.Username = StravaUsername;
    Strava.Config.Password = StravaPassword;
    Strava.Cookies = new();

    signedIn = await Strava.AuthenticateAsync();

    if (!signedIn)
    {
      notifier_.NotifyUser("There was a problem signing in to Strava", autoDismiss: true);
      return;
    }

    // Save login
    await UpdateSettingsAsync(settings =>
    {
      settings.StravaUsername = StravaUsername;
      settings.StravaPassword = StravaPassword;
      settings.StravaCookies = Strava.Cookies;
    });
  }

  public virtual async Task HandleStravaLogoutClicked()
  {
    await Strava.LogoutAsync();

    StravaUsername = null;
    StravaPassword = null;

    await UpdateSettingsAsync(settings =>
    {
      settings.StravaUsername = StravaUsername;
      settings.StravaPassword = StravaPassword;
      settings.StravaCookies = Strava.Cookies;
    });
    
    notifier_.NotifyUser("Signed out of Strava", autoDismiss: true);
  }

  public void HandleSyncNowClicked() => _ = Task.Run(FitEdit.Sync);

  public void HandleFullSyncClicked()
  {
    FitEdit.LastSync = default;
    _ = Task.Run(FitEdit.Sync);
  }

  private async Task UpdateSettingsAsync(Action<AppSettings> action)
  {
    AppSettings? settings = await db_.GetAppSettingsAsync().AnyContext() ?? new AppSettings();
    if (settings is null) { return; }
    action(settings);
    await db_.InsertOrUpdateAsync(settings).AnyContext();
  }
}
