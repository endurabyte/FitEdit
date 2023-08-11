using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dynastream.Fit;
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
  Dauer.Model.Authorization? Authorization { get; set; }

  Task<bool> AuthenticateClientSideAsync(string email, string password, CancellationToken ct = default);
  Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);

  /// <summary>
  /// Return PKCE verifier. Null if usePkce is false.
  /// </summary>
  Task<string?> AuthenticateWithMagicLink(string email, bool usePkce, string redirectUri);

  /// <summary>
  /// Return access token
  /// </summary>
  Task<string?> ExchangeCodeForSession(string? verifier, string? code);

  Task<bool> LogoutAsync();
  Task<bool> LogoutGarminAsync();
}

public class NullSupabaseAdapter : ISupabaseAdapter
{
  public bool IsAuthenticated => false;
  public bool IsAuthenticatedWithGarmin => false;
  public Authorization? Authorization { get; set; }

  public Task<bool> AuthenticateClientSideAsync(string email, string password, CancellationToken ct = default) => Task.FromResult(false);
  public Task<string?> AuthenticateWithMagicLink(string email, bool usePkce, string redirectUri) => Task.FromResult(null as string);
  public Task<string?> ExchangeCodeForSession(string? verifier, string? code) => Task.FromResult(null as string);
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> LogoutAsync() => Task.FromResult(false);
  public Task<bool> LogoutGarminAsync() => Task.FromResult(false);
}

public class SupabaseAdapter : ReactiveObject, ISupabaseAdapter
{
  private readonly global::Supabase.Client client_;
  private readonly ILogger<SupabaseAdapter> log_;
  private readonly IDatabaseAdapter db_;

  [Reactive] public bool IsAuthenticated { get; private set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }
  [Reactive] public Dauer.Model.Authorization? Authorization { get; set; }

  public SupabaseAdapter(ILogger<SupabaseAdapter> log, IDatabaseAdapter db, string url, string key)
  {
    log_ = log;
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
      log_.LogInformation("Realtime debug: {@sender} {@message} {@debug}", sender, message, exception);
    });

    client_.Auth.AddDebugListener((message, exception) =>
    {
      log_.LogInformation("Auth debug: {@message} {@debug}", message, exception);
    });

    client_.Auth.AddStateChangedListener(async (sender, changed) =>
    {
      log_.LogInformation("Auth state changed to {@changed}", changed);

      IsAuthenticated = await IsAuthenticatedAsync();

      await SyncAuthorization();
    });

    this.ObservableForProperty(x => x.Authorization).Subscribe(_ =>
    {
      persistence.Authorization = Authorization;
      client_.Auth.LoadSession();
    });

    db_.ObservableForProperty(x => x.Ready).Subscribe(async _ =>
    {
      Authorization = await LoadCachedAuthorization();

      var t = Task.Run(async () =>
      {
        await client_.InitializeAsync();

        RealtimeChannel? channel = client_.Realtime.Channel("realtime", "public", "GarminUser");

        channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.All, (_, change) =>
        {
          var model = change.Model<Model.GarminUser>();
          SyncAuthorization(model);
        });

        await channel.Subscribe();
      });
    });
  }

  private async Task<Dauer.Model.Authorization?> LoadCachedAuthorization()
  {
    if (db_ == null) { return null; }
    if (!db_.Ready) { return null; }

    Dauer.Model.Authorization result = await db_.GetAuthorizationAsync("Dauer.Api");
    if (result == null) { return null; }

    return new Dauer.Model.Authorization
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
      Dauer.Model.Log.Error(e);
      return false;
    }
  }

  public async Task<bool> AuthenticateClientSideAsync(string email, string password, CancellationToken ct = default)
  {
    await Task.CompletedTask;
    //await client_.Auth.ResetPasswordForEmail(email);
    Session? session = await client_.Auth.SignInWithPassword(email, password);

    bool ok = session?.AccessToken != null;
    return ok;
  }

  public async Task<string?> AuthenticateWithMagicLink(string email, bool usePkce, string redirectUri)
  {
    //await Task.CompletedTask;
    //return null;
    PasswordlessSignInState? signInState = await client_.Auth.SignInWithOtp(new SignInWithPasswordlessEmailOptions(email)
    {
      EmailRedirectTo = redirectUri,
      FlowType = usePkce ? OAuthFlowType.PKCE : OAuthFlowType.Implicit,
    });

    return signInState?.PKCEVerifier;
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
    var garminUser = await client_.Postgrest.Table<Model.GarminUser>().Get();
    if (garminUser?.Model is null) { return false; }
    SyncAuthorization(garminUser.Model);
    return true;
  }

  public void SyncAuthorization(Model.GarminUser? user) 
  {
    if (user is null) { return; }
    IsAuthenticatedWithGarmin = !string.IsNullOrEmpty(user.AccessToken);
  }
}
