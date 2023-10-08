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
  [Reactive] public DateTime LastSync { get; set; }

  private RealtimeChannel? userChannel_;
  private RealtimeChannel? stravaUserChannel_;
  private RealtimeChannel? garminUserChannel_;
  private RealtimeChannel? activityChannel;

  private readonly SemaphoreSlim syncSem_ = new(1, 1);
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
      _ = Task.Run(SyncRecent);
    });

    db_.ObservableForProperty(x => x.Ready, skipInitial: false).Subscribe(async change =>
    {
      var settings = await db_.GetAppSettingsAsync();
      LastSync = settings.LastSynced ?? default;

      Authorization = await LoadCachedAuthorization();
      _ = Task.Run(SyncRecent);
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
    Subscribe(nameof(Model.User), ref userChannel_, change => Sync(change.Model<Model.User>()));
    Subscribe(nameof(StravaUser), ref stravaUserChannel_, change => Sync(change.Model<Model.StravaUser>()));
    Subscribe(nameof(GarminUser), ref garminUserChannel_, change => Sync(change.Model<Model.GarminUser>()));
    Subscribe(nameof(Activity), ref activityChannel, change =>
    {
      var old = change.OldModel<Activity>();
      var activity = change.Model<Activity>();

      log_.LogInformation($"Got Activity notification {{@activity}}", activity);

      _ = Task.Run(async () =>
      {
        if (change?.Payload?.Data?.Type == global::Supabase.Realtime.Constants.EventType.Delete)
        {
          await HandleActivityDeleted(old?.Id);
          return;
        }

        await HandleActivityAddedOrUpdated(activity).AnyContext();
      });
    });

    _ = Task.Run(SyncRecent);
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

  private async Task HandleActivityDeleted(string? id)
  {
    if (id is null) { return; }
    LocalActivity? act = await db_.GetActivityAsync(id);


    UiFile? uif = fileService_.Files.FirstOrDefault(uif => uif.Activity?.Id == id);
    if (uif != null) 
    {
      fileService_.Files.Remove(uif);
    }

    await fileService_.DeleteAsync(act);
  }

  private async Task HandleActivityAddedOrUpdated(Activity? activity)
  {
    if (activity == null) { return; }

    LocalActivity toWrite = activity.MapLocalActivity();
    bool exists = false;
    var uif = new UiFile();

    // Prevent downloading the same new upload more than once.
    // I've seen as many as 5 rapid notifications from Garmin for the same activity upload.
    // Proceed even if we didn't enter the semaphore, since we prefer duplicate downloads over missing any.
    await updateSem_.RunAtomically(async () =>
    {
      LocalActivity? la = await fileService_.GetByIdOrStartTimeAsync(toWrite.Id, toWrite.StartTime);
      exists = la != null;
      uif.Activity = exists ? la : toWrite;
    }, nameof(HandleActivityAddedOrUpdated), TimeSpan.FromMinutes(1));

    if (exists)
    {
      await UpdateExistingActivity(uif, toWrite).AnyContext();
    }
    else
    {
      await AddNewActivity(uif, activity?.BucketUrl).AnyContext();
    }
  }

  /// <summary>
  /// Remove leading bucket name
  /// "activity-files/<userId>/<activityId>" => "<userId>/<activityId>"
  /// </summary>
  private static string RemoveLeadingBucketName(string bucketUrl) => bucketUrl[(bucketUrl.IndexOf("/") + 1)..];

  private async Task AddNewActivity(UiFile uiFile, string? bucketUrl)
  {
    LocalActivity? toWrite = uiFile.Activity;
    if (toWrite is null) { return; }

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
        log_.LogError("Could not download Activity: {@json}", json);
        return;
      }

      toWrite.File = new FileReference(toWrite.Id, bytes);
    }

    bool ok = await fileService_.CreateAsync(toWrite);
    fileService_.Add(uiFile);

    if (ok) { log_.LogInformation("Created activity {@activity}", toWrite); }
    else { log_.LogInformation("Could not create activity {@activity}", toWrite); }
  }

  private async Task UpdateExistingActivity(UiFile? existing, LocalActivity update)
  {
    if (existing == null) { return; }
    if (existing.Activity == null) { return; }

    LocalActivity? known = existing.Activity;

    // Merge data
    known.Name = update.Name;
    known.Description = update.Description;
    known.StartTime = update.StartTime;
    known.SourceId = update.SourceId;
    known.Source = update.Source;

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

  public async Task Sync() => await SyncRecent();

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

    _ = Task.Run(SyncFull);
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

      _ = Task.Run(SyncFull);
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

  public async Task<bool> UpdateAsync(LocalActivity? act)
  {
    if (act is null) { return false; }
    if (act.File is null) { return false; }
    if (!IsAuthenticated) { return false; }
    if (Authorization?.Sub is null) { return false; }
    if (!IsActive) { return false; } // Should we nudge the user to subscribe?

    string bucketUrl = $"{Authorization?.Sub}/{act.Id}";

    try
    {
      act.LastUpdated = DateTime.UtcNow;
      act.BucketUrl = act.File.Bytes.Length == 0 
        ? null 
        : await client_.Storage
          .From("activity-files")
          .Upload(act.File.Bytes, bucketUrl);

    var activity = act.MapActivity();
    activity.SupabaseUserId = Authorization?.Sub;

      await client_.Postgrest.Table<Model.Activity>()
        .Upsert(activity)
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

  private async Task SyncFull()
  {
    LastSync = default; // Get everything in the DB not just recent changes
    await SyncRecent();
  }

  /// <summary>
  /// Query for activity changes we missed while offline: adds, updates, and deletes.
  /// Sync up to 1 year
  /// </summary>
  private async Task SyncRecent()
  {
    if (!db_.Ready) { return; }

    // Only run this method one at a time
    await syncSem_.RunAtomically(async () =>
    {
      // Sync up to 1 year
      DateTime before = DateTime.UtcNow;
      DateTime after = before - TimeSpan.FromDays(365);

      List<object> ids = (await fileService_
        .GetAllActivityIdsAsync(after, before)
        .AnyContext())
        .Cast<object>()
        .ToList();

      await SyncAddsAndUpdates(LastSync);
      await SyncDeletes(before, after, ids);
      LastSync = DateTime.UtcNow;

    }, nameof(SyncRecent));
  }

  private async Task SyncAddsAndUpdates(DateTime lastSync)
  {
    // Get activities updated since the last sync.
    // Row-level security ensures we only get the current user's activities.
    var activities = await client_.Postgrest.Table<Model.Activity>()
      .Where(a => a.LastUpdated > lastSync)
      .Get()
      .AnyContext();

    // Redundant defensive filter
    foreach (Activity activity in activities.Models)
    {
      await HandleActivityAddedOrUpdated(activity).AnyContext();
    }
  }

  private async Task SyncDeletes(DateTime before, DateTime after, List<object> ids)
  {
    if (client_.Auth.CurrentSession == null) { return; }

    // All remote activity ids in the given time frame
    var inTimeSpan = await client_.Postgrest.Table<Model.Activity>()
      .Where(a => a.LastUpdated > after && a.LastUpdated < before)
      .Order(a => a.LastUpdated!, Postgrest.Constants.Ordering.Descending)
      .Select(nameof(Activity.Id))
      .Get()
      .AnyContext();

    // All remote activity ids with null timestamps
    var nullTimestamp = await client_.Postgrest.Table<Model.Activity>()
      .Filter(nameof(Activity.LastUpdated), Postgrest.Constants.Operator.Equals, null)
      .Select(nameof(Activity.Id))
      .Get()
      .AnyContext();

    // All remote activity ids in the given time frame or with null timestamps
    List<string> remoteIds = inTimeSpan.Models
      .Concat(nullTimestamp.Models)
      .Select(a => a.Id)
      .ToList();

    // Activity  ids that are in our local DB but not in the remote DB. These are the deleted IDs.
    List<string> deletedIds = ids
      .Except(remoteIds)
      .Cast<string>()
      .ToList();

    // Zero remote IDs happens when there is no session or auth problem, despite HTTP 200.
    // User probably did not delete all their activities at once
    if (remoteIds.Count == 0 && ids.Count > 1) { return; }
    
    foreach (string deletedId in deletedIds)
    {
      await HandleActivityDeleted(deletedId);
    }
  }

  public async Task<bool> DeleteAsync(LocalActivity? act)
  {
    if (act is null) { return false; }
    
    await client_.Postgrest.Table<Model.Activity>()
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
