using System.Text;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Ui.Supabase.Model;
using Dauer.Ui.ViewModels;
using Microsoft.Extensions.Logging;
using Postgrest;
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
  private readonly IFileService fileService_;
  private readonly IDatabaseAdapter db_;

  [Reactive] public bool IsConnected { get; private set; }
  [Reactive] public bool IsAuthenticated { get; private set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }
  [Reactive] public Authorization? Authorization { get; set; }

  private RealtimeChannel? garminUserChannel_;
  private RealtimeChannel? garminActivityChannel_;

  private readonly SemaphoreSlim updateSem_ = new(1, 1);

  public SupabaseAdapter(
    ILogger<SupabaseAdapter> log,
    IFileService fileService,
    IDatabaseAdapter db,
    string url,
    string key)
  {
    log_ = log;
    fileService_ = fileService;
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
      //log_.LogDebug("Realtime debug: {@sender} {@message} {@debug}", sender, message, exception);
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
      //log_.LogDebug("Auth debug: {@message} {@debug}", message, exception);
    });

    client_.Auth.AddStateChangedListener(async (sender, changed) =>
    {
      log_.LogInformation("Auth state changed to {@changed}", changed);

      IsAuthenticated = await IsAuthenticatedAsync();

      await SyncAuthorization();
    });

    db_.ObservableForProperty(x => x.Ready, skipInitial: false).Subscribe(async change =>
    {
      Authorization = await LoadCachedAuthorization();
      _ = Task.Run(GetRecentActivities);
    });

    this.ObservableForProperty(x => x.Authorization).Subscribe(_ =>
    {
      persistence.Authorization = Authorization;
      InitClient();
    });

    // When files are deleted from the app, also delete their database record and bucket file
    fileService_.Deleted.Subscribe(async act => await DeleteActivity(act).AnyContext());
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
    try
    {
      garminUserChannel_?.Unsubscribe();
      garminActivityChannel_?.Unsubscribe();
    }
    catch (Exception e)
    {
      log_.LogError($"Error unsubscribing channel: {{@e}}", e);
    }

    garminUserChannel_ = client_.Realtime.Channel("realtime", "public", "GarminUser");
    garminActivityChannel_ = client_.Realtime.Channel("realtime", "public", "GarminActivity");

    garminUserChannel_.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (_, change) =>
    {
      var user = change.Model<Model.GarminUser>();
      SyncAuthorization(user);
    });

    garminActivityChannel_.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (channel, change) =>
    {
      var activity = change.Model<Model.GarminActivity>();
      log_.LogInformation($"Got GarminActivity notification {{@activity}}", activity);

      _ = Task.Run(async () => await HandleActivityNotification(activity).AnyContext());
    });

    garminUserChannel_.Subscribe();
    garminActivityChannel_.Subscribe();

    _ = Task.Run(GetRecentActivities);
  }

  private async Task HandleActivityNotification(GarminActivity? activity)
  {
    if (activity == null) { return; }

    DauerActivity toWrite = activity.MapDauerActivity();
    UiFile? uiFile = fileService_.Files.FirstOrDefault(f => f?.Activity?.Id == toWrite.Id);

    if (uiFile != null)
    {
      await UpdateExistingActivity(uiFile, toWrite).AnyContext();
      return;
    }

    await AddNewActivity(toWrite, uiFile, activity?.BucketUrl).AnyContext();
  }

  /// <summary>
  /// Remove leading bucket name
  /// "activity-files/<userId>/<activityId>" => "<userId>/<activityId>"
  /// </summary>
  private static string RemoveLeadingBucketName(string bucketUrl) => bucketUrl[(bucketUrl.IndexOf("/") + 1)..];

  private async Task AddNewActivity(DauerActivity toWrite, UiFile? uiFile, string? bucketUrl)
  {
    if (bucketUrl != null)
    {
      string path = RemoveLeadingBucketName(bucketUrl);

      byte[] bytes = await client_.Storage
        .From("activity-files")
        .Download(path, (sender, progress) =>
        {
          log_.LogInformation("File download progress: {@url} {@percent}", toWrite.BucketUrl, progress);
        });

      if (bytes.Length < 200)
      {
        // Response is a JSON object containing an error string
        string json = Encoding.ASCII.GetString(bytes);
        log_.LogError("Could not download GarminActivity: {@json}", json);
        return;
      }

      toWrite.File = new FileReference(toWrite.Id, bytes);
    }

    bool ok = await fileService_.CreateAsync(toWrite);

    if (ok) { log_.LogInformation("Created activity {@activity}", toWrite); }
    else { log_.LogInformation("Could not create activity {@activity}", toWrite); }


    uiFile ??= new UiFile { Activity = toWrite, };

    // Trigger update
    fileService_.Files.Remove(uiFile);
    fileService_.Files.Add(uiFile);
  }

  private async Task UpdateExistingActivity(UiFile? existing, DauerActivity update)
  {
    if (existing == null) { return; }
    if (existing.Activity == null) { return; }

    DauerActivity? known = existing.Activity;

    // Merge data
    known.Name = update.Name;
    known.Description = update.Description;

    bool ok = await fileService_.UpdateAsync(known);

    if (ok) { log_.LogInformation("Updated activity {@activity}", known); }
    else { log_.LogInformation("Could not update activity {@activity}", known); }

    return;
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

  /// <summary>
  /// Query for activities we missed while offline
  /// </summary>
  private async Task GetRecentActivities()
  {
    if (!db_.Ready) { return; }

    bool entered = !await updateSem_.WaitAsync(TimeSpan.Zero).AnyContext();

    if (!entered)
    {
      return;
    }

    try
    {
      List<object> ids = (await fileService_
        .GetAllActivityIdsAsync()
        .AnyContext())
        .Cast<object>()
        .ToList();

      var activities = await client_.Postgrest.Table<Model.GarminActivity>()
        // Filter out known ids
        .Not("Id", Postgrest.Constants.Operator.In, ids)
        .Get()
        .AnyContext();

      if (activities == null) { return; }

      // Redundant defensive filter
      foreach (var activity in activities.Models.Where(a => !ids.Contains(a.Id)))
      {
        await HandleActivityNotification(activity).AnyContext();
      }
    }
    catch (Exception e)
    {
      log_.LogError("Exception getting recent GarminActivities: {@e}", e);
    }
    finally
    {
      updateSem_.Release();
    }
  }

  private async Task DeleteActivity(DauerActivity act)
  {
    await client_.Postgrest.Table<Model.GarminActivity>()
      .Filter("Id", Postgrest.Constants.Operator.Equals, act.Id)
      .Delete()
      .AnyContext();

    if (Authorization?.Sub is null) { return; }

    string supabaseUserId = Authorization.Sub;
    string bucketUrl = $"{supabaseUserId}/{act.Id}";

    await client_.Storage
      .From("activity-files")
      .Remove(bucketUrl)
      .AnyContext();
  }
}
