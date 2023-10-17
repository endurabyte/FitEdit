using System.Reactive.Linq;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Model.GarminConnect;
using Dauer.Model.Strava;
using Dauer.Model.Validators;
using Dauer.Model.Web;
using Dauer.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface ISettingsViewModel
{

}

public class DesignSettingsViewModel : SettingsViewModel
{
  public DesignSettingsViewModel() : base(
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
    await Garmin.AuthenticateAsync();
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
  [Reactive] public string? GarminUsername { get; set; }
  [Reactive] public string? GarminPassword { get; set; }

  [Reactive] public bool GarminManualLogin { get; set; }
  [Reactive] public string? GarminSsoId { get; set; }
  [Reactive] public string? GarminSessionId { get; set; }

  [Reactive] public string? StravaUsername { get; set; }
  [Reactive] public string? StravaPassword { get; set; }
  [Reactive] public string Message { get; set; } = "Please enter an email address and click Sign In";

  public string PaymentPortalUrl => $"https://billing.stripe.com/p/login/5kA8Ap72G1oebZK9AA?prefilled_email={FitEdit.Username}";
  //public string PaymentPortalUrl => $"https://billing.stripe.com/p/login/test_6oE7vA7Yzfwg1eo144?prefilled_email={FitEdit.Username}";
  public string SignUpUrl => $"https://www.fitedit.io/pricing.html";

  private CancellationTokenSource authCancelCts_ = new();
  private readonly IDatabaseAdapter db_;
  private readonly IEmailValidator emailValidator_;
  private readonly IPhoneValidator phoneValidator_;
  private readonly IBrowser browser_;

  public SettingsViewModel(
    IDatabaseAdapter db,
    IFitEditService fitEdit,
    IGarminConnectClient garmin,
    IStravaClient strava,
    IEmailValidator emailValidator,
    IPhoneValidator phoneValidator,
    IBrowser browser
  )
  {
    db_ = db;
    FitEdit = fitEdit;
    Garmin = garmin;
    Strava = strava;
    emailValidator_ = emailValidator;
    phoneValidator_ = phoneValidator;
    browser_ = browser;

    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ => await LoadSettings_());
    _ = Task.Run(LoadSettings_);

    this.ObservableForProperty(x => x.Message).Subscribe(msg => Log.Info(msg.Value));

    FitEdit.ObservableForProperty(x => x.IsAuthenticatedWithGarmin)
      .Subscribe(_ =>
      {
        Message = FitEdit.IsAuthenticatedWithGarmin
          ? "Successfully connected to Garmin!"
          : "Disconnected from Garmin";
      });

    FitEdit.ObservableForProperty(x => x.IsAuthenticatedWithStrava)
      .Subscribe(_ =>
      {
        Message = FitEdit.IsAuthenticatedWithStrava
          ? "Successfully connected to Strava!"
          : "Disconnected from Strava";
      });

    FitEdit.ObservableForProperty(x => x.IsAuthenticated)
      .Subscribe(_ =>
      {
        Message = FitEdit.IsAuthenticated
          ? "Sign in complete!"
          : "Signed out";
      });

    FitEdit.ObservableForProperty(x => x.GarminCookies)
      .Subscribe(async _ =>
      {
        Message = "Received new cookies from the FitEdit browser extension.";

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

        GarminManualLogin = GarminSsoId != null && GarminSessionId != null;
        await LoginWithGarminAsync();
      });

    garmin.ObservableForProperty(x => x.Cookies).Subscribe(async _ =>
    {
      await UpdateSettingsAsync(settings => settings.GarminCookies = Garmin.Cookies);
    });

    FitEdit.ObservableForProperty(x => x.LastSync).Subscribe(async _ =>
    {
      await UpdateSettingsAsync(settings =>
      {
        settings.LastSynced = FitEdit.LastSync;
      });
    });
  }

  private async Task LoadSettings_()
  {
    if (!db_.Ready) { return; }

    AppSettings? settings = await db_.GetAppSettingsAsync().AnyContext() ?? new AppSettings();
    GarminUsername = settings.GarminUsername;
    GarminPassword = settings.GarminPassword;
    GarminSsoId = settings.GarminSsoId;
    GarminSessionId = settings.GarminSessionId;
    Garmin.Cookies = settings.GarminCookies;
    GarminManualLogin = GarminSsoId != null && GarminSessionId != null;

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
      Message = "We sent you an email. " +
        "If you're a new user, it has a link. " +
        "If you're a returning user, it has a code and a link. " +
        "Enter the code or open the link on this device within 5 minutes.";
    } else if (phoneValidator_.IsValid(username))
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
      Message = ok ? "Signed out of FitEdit" : "There was a problem signing out of FitEdit";
    });
  }

  public void HandleGarminAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminAuthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      await FitEdit.AuthorizeGarminAsync();
      Message = FitEdit.IsAuthenticatingWithGarmin
        ? "Check your web browser. We've opened the Garmin authorize page" 
        : "There was a problem authorizing with Garmin";
    });
  }

  public void HandleGarminDeauthorizeClicked()
  {
    Log.Info($"{nameof(HandleGarminDeauthorizeClicked)}");
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.DeauthorizeGarminAsync();
      Message = ok ? "Deauthorized Garmin" : "There was a problem deauthorizing Garmin";
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

  public virtual void HandleManagePaymentsClicked()
  {
    _ = Task.Run(async () =>
    {
      await browser_.OpenAsync(PaymentPortalUrl);
      Message = "Check your web browser. We've opened a page to manage your payment.";
    });
  }

  public virtual void HandleSignUpClicked()
  {
    _ = Task.Run(async () =>
    {
      await browser_.OpenAsync(SignUpUrl);
      Message = "Check your web browser. We've opened a page to sign up.";
    });
  }

  public void HandleStravaAuthorizeClicked()
  {
    _ = Task.Run(async () =>
    {
      await FitEdit.AuthorizeStravaAsync();
      Message = FitEdit.IsAuthenticatingWithStrava
        ? "Check your web browser. We've opened the Strava authorize page" 
        : "There was a problem authorizing with Strava";
    });
  }
  
  public void HandleStravaDeauthorizeClicked()
  {
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.DeauthorizeStravaAsync();
      Message = ok ? "Deauthorized Strava" : "There was a problem deauthorizing Strava";
    });
  }

  public virtual async Task HandleGarminLoginClicked() => await LoginWithGarminAsync();

  public async Task LoginWithGarminAsync()
  {
    bool signedIn = false; 

    if (GarminManualLogin)
    {
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
    }
    else
    {
      if (GarminUsername == null || GarminPassword == null) { return; }

      Garmin.Config.SsoId = null;
      Garmin.Config.SessionId = null;
      Garmin.Config.JwtId = null;

      Garmin.Config.Username = GarminUsername;
      Garmin.Config.Password = GarminPassword;
      Garmin.Cookies = new();
      signedIn = await Garmin.AuthenticateAsync();
    }

    Message = signedIn ? "Signed in to Garmin" : "There was a problem signing in to Garmin";

    if (!signedIn)
    {
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

    GarminUsername = null;
    GarminPassword = null;
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

    Message = "Signed out of Garmin";
  }

  public async Task HandleTermsClicked() => await browser_.OpenAsync("https://www.fitedit.io/support/integration-signin-terms.html");

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

    Message = signedIn ? "Signed in to Strava" : "There was a problem signing in to Strava";

    if (!signedIn)
    {
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
    
    Message = "Signed out of Strava";
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
