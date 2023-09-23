#nullable enable
using Dauer.Model;

namespace Dauer.Ui.Model.Supabase;

public class NullSupabaseAdapter : ISupabaseAdapter
{
  public bool IsAuthenticated => false;
  public bool IsAuthenticatedWithGarmin => false;
  public bool IsAuthenticatedWithStrava => false;
  public bool IsActive => false;
  public Authorization? Authorization { get; set; }

  public Task<bool> SignInWithEmailAndPassword(string email, string password, CancellationToken ct = default) => Task.FromResult(false);
  public Task<string?> SignInWithOtp(string username, bool usePkce, string redirectUri) => Task.FromResult(null as string);
  public Task<string?> ExchangeCodeForSession(string? verifier, string? code) => Task.FromResult(null as string);
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> VerifyOtpAsync(string? username, string token) => Task.FromResult(false);
  public Task<bool> LogoutAsync() => Task.FromResult(false);

  public Task<bool> UpdateAsync(DauerActivity? act) => Task.FromResult(false);
  public Task<bool> DeleteAsync(DauerActivity? act) => Task.FromResult(false);
}
