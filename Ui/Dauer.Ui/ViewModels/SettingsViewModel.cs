using System.Reactive.Linq;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Model.GarminConnect;
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

  public override Task HandleGarminLoginClicked()
  {
    IsLoggedInWithGarmin = true;
    return Task.CompletedTask;
  }

  public override Task HandleGarminLogoutClicked()
  {
    IsLoggedInWithGarmin = false;
    return Task.CompletedTask;
  }
}

public class SettingsViewModel : ViewModelBase, ISettingsViewModel
{
  public IFitEditService FitEdit { get; set; }

  [Reactive] public string Otp { get; set; } = "";
  [Reactive] public string? GarminUsername { get; set; }
  [Reactive] public string? GarminPassword { get; set; }
  [Reactive] public string? StravaUsername { get; set; }
  [Reactive] public string? StravaPassword { get; set; }
  [Reactive] public bool IsLoggedInWithGarmin { get; set; }
  [Reactive] public string Message { get; set; } = "Please enter an email address and click Sign In";

  public string PaymentPortalUrl => $"https://billing.stripe.com/p/login/5kA8Ap72G1oebZK9AA?prefilled_email={FitEdit.Username}";
  //public string PaymentPortalUrl => $"https://billing.stripe.com/p/login/test_6oE7vA7Yzfwg1eo144?prefilled_email={FitEdit.Username}";
  public string SignUpUrl => $"https://www.fitedit.io/pricing.html";

  private CancellationTokenSource authCancelCts_ = new();
  private readonly IDatabaseAdapter db_;
  private readonly IGarminConnectClient garmin_;
  private readonly IEmailValidator emailValidator_;
  private readonly IPhoneValidator phoneValidator_;
  private readonly IBrowser browser_;

  public SettingsViewModel(
    IDatabaseAdapter db,
    IFitEditService fitEdit,
    IGarminConnectClient garmin,
    IEmailValidator emailValidator,
    IPhoneValidator phoneValidator,
    IBrowser browser
  )
  {
    db_ = db;
    FitEdit = fitEdit;
    garmin_ = garmin;
    emailValidator_ = emailValidator;
    phoneValidator_ = phoneValidator;
    browser_ = browser;

    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ => await LoadSettings_());
    _ = Task.Run(LoadSettings_);

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
  }

  private async Task LoadSettings_()
  {
    if (!db_.Ready) { return; }

    AppSettings? settings = await db_.GetAppSettingsAsync().AnyContext() ?? new AppSettings();
    garmin_.AddCookies(settings.GarminCookies);

    GarminUsername = settings.GarminUsername;
    GarminPassword = settings.GarminPassword;
    await LoginWithGarminAsync();
  }

  public void HandleLoginClicked()
  {
    Log.Info($"{nameof(HandleLoginClicked)}");
    Otp = "";
    string? username = FitEdit.Username?.Trim();

    if (emailValidator_.IsValid(username))
    {
      Message = "We sent you an email. " +
        "\nIf you're a new user, it has a link. " +
        "\nIf you're a returning user, it has a code and a link. " +
        "\nEnter the code or open the link on this device within 5 minutes.";
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
        ? "Check your web browser. We've opened a page to Strava" 
        : "There was a problem connecting to Strava";
    });
  }
  
  public void HandleStravaDeauthorizeClicked()
  {
    _ = Task.Run(async () =>
    {
      bool ok = await FitEdit.DeauthorizeStravaAsync();
      Message = ok ? "Disconnected from Strava" : "There was a problem disconnecting from Strava";
    });
  }

  public virtual async Task HandleGarminLoginClicked()
  {
    await LoginWithGarminAsync();
  }

  public async Task LoginWithGarminAsync()
  { 
    IsLoggedInWithGarmin = await garmin_.IsAuthenticatedAsync();

    if (IsLoggedInWithGarmin) 
    {
      return; 
    }

    garmin_.Config.Username = GarminUsername;
    garmin_.Config.Password = GarminPassword;

    IsLoggedInWithGarmin = await garmin_.AuthenticateAsync();

    // Save login
    if (IsLoggedInWithGarmin)
    {
      await UpdateSettingsAsync(settings =>
      {
        settings.GarminUsername = GarminUsername;
        settings.GarminPassword = GarminPassword;
        settings.GarminCookies = garmin_.GetCookies();
      });
    }
  }

  public virtual async Task HandleGarminLogoutClicked()
  {
    GarminUsername = null;
    GarminPassword = null;
    IsLoggedInWithGarmin = false;

    await UpdateSettingsAsync(settings =>
    {
      settings.GarminUsername = null;
      settings.GarminPassword = null;
      settings.GarminCookies = null;
    });
  }

  public async Task HandleGarminLoginInfoClicked() => await browser_.OpenAsync("https://www.fitedit.io/support/about-garmin-login.html");

  private async Task UpdateSettingsAsync(Action<AppSettings> action)
  {
    AppSettings? settings = await db_.GetAppSettingsAsync().AnyContext() ?? new AppSettings();
    if (settings is null) { return; }
    action(settings);
    await db_.InsertOrUpdateAsync(settings).AnyContext();
  }
}
