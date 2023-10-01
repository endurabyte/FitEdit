#nullable enable
namespace Dauer.Services;

public interface IFitEditService
{
  bool IsAuthenticated { get; }
  bool IsAuthenticating { get; }
  bool IsAuthenticatingWithGarmin { get; }
  bool IsAuthenticatedWithGarmin { get; }
  bool IsAuthenticatingWithStrava { get; }
  bool IsAuthenticatedWithStrava { get; }
  bool IsActive { get; }
  bool SupportsPayments { get; }
  string? Username { get; set; }

  Task<bool> AuthenticateAsync(CancellationToken ct = default);
  Task<bool> LogoutAsync(CancellationToken ct = default);
  Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
  Task<bool> VerifyOtpAsync(string token);
  Task AuthorizeGarminAsync(CancellationToken ct = default);
  Task<bool> DeauthorizeGarminAsync(CancellationToken ct = default);

  Task<bool> AuthorizeStravaAsync(CancellationToken ct = default);
  Task<bool> DeauthorizeStravaAsync(CancellationToken ct = default);
}
