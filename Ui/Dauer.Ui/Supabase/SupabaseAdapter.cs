using System.Text;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Ui.Supabase.Model;
using Dauer.Ui.ViewModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Gotrue.Constants;

namespace Dauer.Ui.Supabase;

public interface ISupabaseAdapter
{
  bool IsAuthenticated { get; }
  bool IsAuthenticatedWithGarmin { get; }
  Authorization? Authorization { get; set; }

  Task<bool> SignInWithEmailAndPassword(string email, string password, CancellationToken ct = default);
  Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);

  /// <summary>
  /// Send a one-time password to an email address or phone number.
  /// 
  /// <para/>
  /// If the username is a phone number, it must be in the E.164 format. A OTP will be sent and null is returned.
  ///
  /// <para/>
  /// If the username is an email address, an OTP and a link to <paramref name="redirectUri"/> will be sent. 
  /// If <paramref name="usePkce"/> is true, return PKCE verifier, else null.
  /// </summary>
  Task<string?> SignInWithOtp(string username, bool usePkce, string redirectUri);

  /// <summary>
  /// Return access token
  /// </summary>
  Task<string?> ExchangeCodeForSession(string? verifier, string? code);

  Task<bool> VerifyOtpAsync(string? username, string token);
  Task<bool> LogoutAsync();
}

public class NullSupabaseAdapter : ISupabaseAdapter
{
  public bool IsAuthenticated => false;
  public bool IsAuthenticatedWithGarmin => false;
  public Authorization? Authorization { get; set; }

  public Task<bool> SignInWithEmailAndPassword(string email, string password, CancellationToken ct = default) => Task.FromResult(false);
  public Task<string?> SignInWithOtp(string username, bool usePkce, string redirectUri) => Task.FromResult(null as string);
  public Task<string?> ExchangeCodeForSession(string? verifier, string? code) => Task.FromResult(null as string);
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> VerifyOtpAsync(string? username, string token) => Task.FromResult(false);
  public Task<bool> LogoutAsync() => Task.FromResult(false);
}

public class SupabaseAdapter : ReactiveObject, ISupabaseAdapter
{
  private readonly global::Supabase.Client client_;
  private readonly ILogger<SupabaseAdapter> log_;
  private readonly IFileService file_;
  private readonly IDatabaseAdapter db_;

  [Reactive] public bool IsConnected { get; private set; }
  [Reactive] public bool IsAuthenticated { get; private set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }
  [Reactive] public Authorization? Authorization { get; set; }

  private RealtimeChannel? garminUserChannel_;
  private RealtimeChannel? garminActivityChannel_;

  public SupabaseAdapter(ILogger<SupabaseAdapter> log, IFileService file, IDatabaseAdapter db, string url, string key)
  {
    log_ = log;
    file_ = file;
    db_ = db;

    var persistence = new SessionPersistence(db);

    client_ = new global::Supabase.Client(url, key, new SupabaseOptions
    {
      SessionHandler = persistence,
      AutoRefreshToken = true,
      AutoConnectRealtime = true,
    });

    client_.Realtime.AddDebugHandler((sender, message, exception) =>
    {
      IsConnected = (sender as RealtimeSocket)?.IsConnected ?? false;
      log_.LogDebug("Realtime debug: {@sender} {@message} {@debug}", sender, message, exception);
    });

    this.ObservableForProperty(x => x.IsConnected).Subscribe(_ =>
    {
      if (IsConnected)
      {
        Subscribe();
      }
    });

    client_.Auth.AddDebugListener((message, exception) =>
    {
      log_.LogDebug("Auth debug: {@message} {@debug}", message, exception);
    });

    client_.Auth.AddStateChangedListener(async (sender, changed) =>
    {
      log_.LogInformation("Auth state changed to {@changed}", changed);

      IsAuthenticated = await IsAuthenticatedAsync();

      await SyncAuthorization();
    });

    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ =>
    {
      Authorization = await LoadCachedAuthorization();
    });

    this.ObservableForProperty(x => x.Authorization).Subscribe(_ =>
    {
      persistence.Authorization = Authorization;
      InitClient();
    });
  }

  private void InitClient()
  {
    var t = Task.Run(async () =>
    {
      client_.Auth.LoadSession();
      await client_.InitializeAsync();
    });
  }

  private void Subscribe()
  {
    garminUserChannel_?.Unsubscribe();
    garminActivityChannel_?.Unsubscribe();

    garminUserChannel_ = client_.Realtime.Channel("realtime", "public", "GarminUser");
    garminActivityChannel_ = client_.Realtime.Channel("realtime", "public", "GarminActivity");

    garminUserChannel_.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (_, change) =>
    {
      var user = change.Model<Model.GarminUser>();
      SyncAuthorization(user);
    });

    garminActivityChannel_.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (_, change) =>
    {
      var activity = change.Model<Model.GarminActivity>();
      log_.LogInformation($"Got uploaded Garmin activity {{@activity}}", activity);

      var t = Task.Run(async () =>
      {
        if (activity?.BucketUrl == null) { return; }

        // Remove leading bucket name
        // "activity-files/<userId>/<activityId>" => "<userId>/<activityId>"
        string path = activity.BucketUrl.Substring(activity.BucketUrl.IndexOf("/") + 1);

        byte[] bytes = await client_.Storage
          .From("activity-files")
          .Download(path, (sender, progress) =>
          {
            log_.LogInformation("File download progress: {@url} {@percent}", activity.BucketUrl, progress);
          });

        if (bytes.Length < 200)
        {
          // Response is a JSON object containing an error string
          string json = Encoding.ASCII.GetString(bytes);
          log_.LogError("Could not download GarminActivity: {@json}", json);
          return;
        }

        var file = new BlobFile(activity?.Name ?? "Untitled Activity", bytes);
        DauerActivity? act = activity?.MapDauerActivity();
        if (act == null) { return; }
        act.File = file;
        bool ok = await db_.InsertAsync(act).AnyContext();

        if (ok) { log_.LogInformation("Persisted activity {@activity}", activity); }
        else { log_.LogInformation("Could not persist activity {@activity}", activity); }

        var uiFile = new UiFile { Blob = file, };
        file_.Files.Add(uiFile);
      });
    });

    garminUserChannel_.Subscribe();
    garminActivityChannel_.Subscribe();
  }

  private async Task<Authorization?> LoadCachedAuthorization()
  {
    if (db_ == null) { return null; }
    if (!db_.Ready) { return null; }

    Authorization result = await db_.GetAuthorizationAsync("Dauer.Api");
    if (result == null) { return null; }

    return new Authorization
    {
      AccessToken = result.AccessToken,
      RefreshToken = result.RefreshToken,
      IdentityToken = result.IdentityToken,
      Created = result.Created,
      Expiry = result.Expiry,
      Username = result.Username
    };
  }

  public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
  {
    if (client_.Auth.CurrentSession?.AccessToken == null) { return false; }

    try
    {
      User? user = await client_.Auth.GetUser(client_.Auth.CurrentSession.AccessToken);
      return user?.Aud == "authenticated";
    }
    catch (GotrueException e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<bool> SignInWithEmailAndPassword(string email, string password, CancellationToken ct = default)
  {
    await Task.CompletedTask;
    //await client_.Auth.ResetPasswordForEmail(email);
    Session? session = await client_.Auth.SignInWithPassword(email, password);

    bool ok = session?.AccessToken != null;
    return ok;
  }

  public async Task<string?> SignInWithOtp(string username, bool usePkce, string redirectUri)
  {
    if (PhoneValidator.IsValid(username))
    {
      await client_.Auth.SignInWithOtp(new SignInWithPasswordlessPhoneOptions(username)
      {
        Channel = SignInWithPasswordlessPhoneOptions.MessagingChannel.SMS
      });

      return null;
    }

    if (!EmailValidator.IsValid(username))
    {
      return null;
    }

    PasswordlessSignInState? signInState = await client_.Auth.SignInWithOtp(new SignInWithPasswordlessEmailOptions(username)
    {
      EmailRedirectTo = redirectUri,
      FlowType = usePkce ? OAuthFlowType.PKCE : OAuthFlowType.Implicit,
    });

    return signInState?.PKCEVerifier;
  }

  public async Task<bool> VerifyOtpAsync(string? username, string token)
  {
    if (username == null) { return false; }

    try
    {
      Session? session = username switch
      {
        _ when PhoneValidator.IsValid(username) => await client_.Auth.VerifyOTP(username, token, MobileOtpType.SMS),
        _ when EmailValidator.IsValid(username) => await client_.Auth.VerifyOTP(username, token, EmailOtpType.MagicLink),
        _ => null,
      };

      Authorization = AuthorizationFactory.Create(session?.AccessToken, session?.RefreshToken);
      return !string.IsNullOrEmpty(session?.AccessToken);
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<string?> ExchangeCodeForSession(string? verifier, string? code)
  {
    if (verifier == null || code == null) { return null; }
    Session? session = await client_.Auth.ExchangeCodeForSession(verifier, code);
    return session?.AccessToken;
  }

  public async Task<bool> LogoutAsync()
  {
    await client_.Auth.SignOut();
    return true;
  }

  public async Task<bool> LogoutGarminAsync()
  {
    if (!IsAuthenticated) { return false; }

    try
    {
      var garminUser = await client_.Postgrest.Table<Model.GarminUser>().Get();
      if (garminUser?.Model is null) { return false; }
      SyncAuthorization(garminUser.Model);

      await client_.Postgrest.Table<Model.GarminUser>()
        .Set(x => x.AccessToken!, "")
        .Where(x => x.Id == garminUser.Model.Id)
        .Update();
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }

    return true;
  }

  public async Task<bool> SyncAuthorization()
  {
    if (!IsAuthenticated) { return false; }

    try
    {
      var garminUser = await client_.Postgrest.Table<Model.GarminUser>().Get();
      if (garminUser?.Model is null) { return false; }
      SyncAuthorization(garminUser.Model);
      return true;
    }
    catch (Exception e)
    {
      log_.LogError("Exception getting GarminUser: {@e}", e);
      return false;
    }
  }

  public void SyncAuthorization(Model.GarminUser? user) 
  {
    if (user is null) { return; }
    IsAuthenticatedWithGarmin = !string.IsNullOrEmpty(user.AccessToken);
  }
}
