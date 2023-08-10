using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Supabase;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;

namespace Dauer.Ui;

public interface ISupabaseAdapter
{
  bool IsAuthenticated { get; }

  bool SetAccessToken(string? jwt);
  Task<bool> AuthenticateClientSideAsync(string email, string password, CancellationToken ct = default);
  Task<bool> IsAuthenticatedAsync(string? accessToken, CancellationToken ct = default);

  /// <summary>
  /// Return PKCE verifier. Null if usePkce is false.
  /// </summary>
  Task<string?> SignInWithMagicLink(string email, bool usePkce, string redirectUri);

  /// <summary>
  /// Return access token
  /// </summary>
  Task<string?> ExchangeCodeForSession(string? verifier, string? code);
}

public class SupabaseAdapter : ReactiveObject, ISupabaseAdapter
{
  private readonly Supabase.Client client_;
  private readonly ILogger<SupabaseAdapter> log_;

  [Reactive]
  public bool IsAuthenticated { get; private set; }

  public SupabaseAdapter(ILogger<SupabaseAdapter> log, string url, string key)
  {
    log_ = log;
    client_ = new Supabase.Client(url, key, new SupabaseOptions
    {

    });

    client_.Auth.AddDebugListener((message, exception) =>
    {
      log_.LogInformation("Auth debug: {@message} {@debug}", message, exception);
    });

    client_.Auth.AddStateChangedListener((sender, changed) =>
    {
      log_.LogInformation("Auth state changed to {@changed}", changed);

      IsAuthenticated = changed switch
      {
        AuthState.SignedIn => true,
        AuthState.SignedOut => false,
        AuthState.UserUpdated => true,
        AuthState.PasswordRecovery => false,
        AuthState.TokenRefreshed => true,
        AuthState.Shutdown => false,
        _ => false,
      };
    });

    _ = Task.Run(client_.InitializeAsync);
  }

  public bool SetAccessToken(string? jwt)
  {
    if (jwt == null) { return false; }
    Session? session = client_.Auth.SetAuth(jwt);
    return session?.AccessToken != null;
  }

  public async Task<bool> AuthenticateClientSideAsync(string email, string password, CancellationToken ct = default)
  {
    await client_.Auth.ResetPasswordForEmail(email);
    Session? session = await client_.Auth.SignInWithPassword(email, password);

    bool ok = session?.AccessToken != null;
    return ok;
  }

  public async Task<bool> IsAuthenticatedAsync(string? accessToken, CancellationToken ct = default)
  { 
    if (accessToken == null) { return false; }

    User? user = await client_.Auth.GetUser(accessToken);
    return user?.Aud == "authenticated";
  }

  public async Task<string?> SignInWithMagicLink(string email, bool usePkce, string redirectUri)
  {
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
}
