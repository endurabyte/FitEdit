using System.Text;
using Dauer.Data;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Model.Validators;
using Dauer.Ui.Infra.Supabase.Model;
using Dauer.Ui.Model.Supabase;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Gotrue.Constants;

namespace Dauer.Ui.Infra.Supabase;

public class SupabaseAdapter : ReactiveObject, ISupabaseAdapter
{
  private readonly global::Supabase.Client client_;
  private readonly ILogger<SupabaseAdapter> log_;
  private readonly IFileService fileService_;
  private readonly IDatabaseAdapter db_;
  private readonly IPhoneValidator phoneValidator_;
  private readonly IEmailValidator emailValidator_;

  [Reactive] public bool IsConnected { get; private set; }
  [Reactive] public bool IsAuthenticated { get; private set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }
  [Reactive] public bool IsAuthenticatedWithStrava { get; private set; }
  [Reactive] public bool IsActive { get; private set; }
  [Reactive] public Authorization? Authorization { get; set; }
  [Reactive] public string? GarminCookies { get; private set; }

  private RealtimeChannel? userChannel_;
  private RealtimeChannel? stravaUserChannel_;
  private RealtimeChannel? garminUserChannel_;
  private RealtimeChannel? garminActivityChannel_;

  private readonly SemaphoreSlim updateSem_ = new(1, 1);

  public SupabaseAdapter(
    ILogger<SupabaseAdapter> log,
    IFileService fileService,
    IDatabaseAdapter db,
    IPhoneValidator phoneValidator,
    IEmailValidator emailValidator,
    string url,
    string key)
  {
    log_ = log;
    fileService_ = fileService;
    db_ = db;
    phoneValidator_ = phoneValidator;
    emailValidator_ = emailValidator;
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
        SubscribeAllTables();
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

      await SyncUserInfo();
      _ = Task.Run(GetRecentActivities);
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
    fileService_.Deleted.Subscribe(async act => await DeleteAsync(act).AnyContext());
  }

  private void InitClient()
  {
    var t = Task.Run(async () =>
    {
      client_.Auth.LoadSession();
      await client_.InitializeAsync();
    });
  }

  private void SubscribeAllTables()
  {
    Subscribe("User", ref userChannel_, change => Sync(change.Model<Model.User>()));
    Subscribe("StravaUser", ref stravaUserChannel_, change => Sync(change.Model<Model.StravaUser>()));
    Subscribe("GarminUser", ref garminUserChannel_, change => Sync(change.Model<Model.GarminUser>()));
    Subscribe("GarminActivity", ref garminActivityChannel_, change =>
    {
      var activity = change.Model<Model.GarminActivity>();
      log_.LogInformation($"Got GarminActivity notification {{@activity}}", activity);

      _ = Task.Run(async () => await HandleActivityNotification(activity).AnyContext());
    });

    _ = Task.Run(GetRecentActivities);
  }

  private void Subscribe(string tableName, ref RealtimeChannel? channel, Action<PostgresChangesResponse> handleChange)
  {
    try
    {
      channel?.Unsubscribe();
    }
    catch (Exception e)
    {
      log_.LogError($"Error unsubscribing channel: {{@e}}", e);
    }

    try
    {
      channel = client_.Realtime.Channel("realtime", "public", tableName);
      channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (_, change) => handleChange(change));
      channel?.Subscribe();
    }
    catch (Exception e)
    {
      log_.LogError($"Error subscribing channel: {{@e}}", e);
    }
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
    fileService_.Add(uiFile);
  }

  private async Task UpdateExistingActivity(UiFile? existing, DauerActivity update)
  {
    if (existing == null) { return; }
    if (existing.Activity == null) { return; }

    DauerActivity? known = existing.Activity;

    // Merge data
    known.Name = update.Name;
    known.Description = update.Description;
    known.StartTime = update.StartTime;
    known.SourceId = update.SourceId;

    bool ok = await fileService_.UpdateAsync(known);

    if (ok) { log_.LogInformation("Updated activity {@activity}", known); }
    else { log_.LogInformation("Could not update activity {@activity}", known); }

    return;
  }

  private async Task<Authorization?> LoadCachedAuthorization()
  {
    if (db_ == null) { return null; }
    if (!db_.Ready) { return null; }

    return await db_.GetAuthorizationAsync("Dauer.Api");
  }

  public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
  {
    if (client_.Auth.CurrentSession?.AccessToken == null) { return false; }

    try
    {
      global::Supabase.Gotrue.User? user = await client_.Auth.GetUser(client_.Auth.CurrentSession.AccessToken);
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
    //await client_.Auth.ResetPasswordForEmail(email);
    Session? session = await client_.Auth.SignInWithPassword(email, password);

    bool ok = session?.AccessToken != null;
    return ok;
  }

  public async Task<string?> SignInWithOtp(string username, bool usePkce, string redirectUri)
  {
    if (phoneValidator_.IsValid(username))
    {
      await client_.Auth.SignInWithOtp(new SignInWithPasswordlessPhoneOptions(username)
      {
        Channel = SignInWithPasswordlessPhoneOptions.MessagingChannel.SMS
      });

      return null;
    }

    if (!emailValidator_.IsValid(username))
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
        _ when phoneValidator_.IsValid(username) => await client_.Auth.VerifyOTP(username, token, MobileOtpType.SMS),
        _ when emailValidator_.IsValid(username) => await client_.Auth.VerifyOTP(username, token, EmailOtpType.MagicLink),
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

  public async Task<bool> UpdateAsync(DauerActivity? act)
  {
    if (act is null) { return false; }
    if (act.File is null) { return false; }
    if (!IsAuthenticated) { return false; }
    if (Authorization?.Sub is null) { return false; }
    if (!IsActive) { return false; } // Should we nudge the user to subscribe?

    string bucketUrl = $"{Authorization?.Sub}/{act.Id}";

    try
    {
      act.BucketUrl = await client_.Storage
        .From("activity-files")
        .Upload(act.File.Bytes, bucketUrl);

    var garminActivity = act.MapGarminActivity();
    garminActivity.SupabaseUserId = Authorization?.Sub;

      await client_.Postgrest.Table<Model.GarminActivity>()
        .Upsert(garminActivity)
        .AnyContext();
    }
    catch (Exception e)
    {
      log_.LogError(e, "Could not update activity");
      return false;
    }

    return true;
  }

  private async Task SyncUserInfo()
  {
    if (!IsAuthenticated) { return; }

    try
    {
      var user = await client_.Postgrest.Table<Model.User>().Get();
      Sync(user?.Model);

      var stravaUser = await client_.Postgrest.Table<Model.StravaUser>().Get();
      Sync(stravaUser?.Model);

      var garminUser = await client_.Postgrest.Table<Model.GarminUser>().Get();
      Sync(garminUser?.Model);
    }
    catch (Exception e)
    {
      log_.LogError("Exception in SyncUserInfo: {@e}", e);
    }
  }

  private bool Sync(Model.User? user)
  {
    if (user is null) { return false; }
    IsActive = user.IsActive == true;
    return true;
  }

  private bool Sync(Model.StravaUser? user)
  {
    if (user is null) { return false; }
    IsAuthenticatedWithStrava = !string.IsNullOrEmpty(user.AccessToken);
    return true;
  }

  private bool Sync(Model.GarminUser? user) 
  {
    if (user is null) { return false; }
    IsAuthenticatedWithGarmin = !string.IsNullOrEmpty(user.AccessToken);
    GarminCookies = user.Cookies;
    return true;
  }

  /// <summary>
  /// Query for activities we missed while offline
  /// </summary>
  private async Task GetRecentActivities()
  {
    if (!db_.Ready) { return; }

    bool entered = await updateSem_.WaitAsync(TimeSpan.Zero).AnyContext();

    if (!entered)
    {
      return;
    }

    AppSettings? settings = await db_.GetAppSettingsAsync().AnyContext() ?? new AppSettings();
    DateTime lastSync = settings.LastSynced ?? DateTime.UtcNow - TimeSpan.FromDays(7);
    settings.LastSynced = DateTime.UtcNow;
    await db_.InsertOrUpdateAsync(settings);

    try
    {
      DateTime before = DateTime.UtcNow;
      DateTime after = before - TimeSpan.FromDays(30);

      List<object> ids = (await fileService_
        // Get up to last 30 days
        .GetAllActivityIdsAsync(after, before)
        .AnyContext())
        .Cast<object>()
        .ToList();

      // We make two requests to the database:
      // First, we want only new activities (i.e. whose IDs we don't already know, within the last 30 days)
      // Then, we want updated activities. (activities we know about but were updated since the last sync)
      // Row-level security ensures we only get the user's activities.

      // Get user's new activities. We label them "possible" because we'll verify each against the DB.
      var possiblyNewActivities = await client_.Postgrest.Table<Model.GarminActivity>()
        .Where(a => a.LastUpdated > after && a.LastUpdated < before)
        .Not("Id", Postgrest.Constants.Operator.In, ids)
        .Get()
        .AnyContext();

      Dictionary<string, bool> existing = new();
      await Parallel.ForEachAsync(possiblyNewActivities.Models, async (act, ct) => existing[act.Id] = await fileService_.ActivityExistsAsync(act.Id));

      List<GarminActivity> definitelyNewActivities = possiblyNewActivities.Models
        .Where(act => !existing[act.Id])
        .ToList();
      
      // Get user's updated activities
      var updatedActivities = await client_.Postgrest.Table<Model.GarminActivity>()
        .Where(a => a.LastUpdated > lastSync)
        .Get()
        .AnyContext();

      List<GarminActivity> activities = definitelyNewActivities
        .Concat(updatedActivities.Models)
        .ToList();

      // Redundant defensive filter
      foreach (var activity in activities)
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

  public async Task<bool> DeleteAsync(DauerActivity? act)
  {
    if (act is null) { return false; }
    
    await client_.Postgrest.Table<Model.GarminActivity>()
      .Filter("Id", Postgrest.Constants.Operator.Equals, act.Id)
      .Delete()
      .AnyContext();

    if (Authorization?.Sub is null) { return false; }

    string supabaseUserId = Authorization.Sub;
    string bucketUrl = $"{supabaseUserId}/{act.Id}";

    await client_.Storage
      .From("activity-files")
      .Remove(bucketUrl)
      .AnyContext();

    return true;
  }
}
