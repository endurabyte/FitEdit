#nullable enable
using Dauer.Model;

namespace Dauer.Ui.Model.Supabase;

public interface ISupabaseAdapter
{
  bool IsAuthenticated { get; }
  bool IsAuthenticatedWithGarmin { get; }
  bool IsAuthenticatedWithStrava { get; }
  bool IsActive { get; }
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

  Task<bool> UpdateAsync(DauerActivity? act);
  Task<bool> DeleteAsync(DauerActivity? act);
}