#nullable enable
namespace Dauer.Services;

public class NullFitEditService : IFitEditService
{
  public bool IsAuthenticated => false;
  public bool IsAuthenticating => false;
  public bool IsAuthenticatedWithGarmin => false;
  public bool IsAuthenticatingWithGarmin => false;
  public bool IsAuthenticatedWithStrava => false;
  public bool IsAuthenticatingWithStrava => false;
  public bool IsActive => false;
  public string? Username { get; set; } = "fake@fake.com";

  public Task<bool> AuthenticateAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> LogoutAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(true);
  public Task<bool> VerifyOtpAsync(string token) => Task.FromResult(true);
  public Task AuthorizeGarminAsync(CancellationToken ct = default) => Task.CompletedTask;
  public Task<bool> DeauthorizeGarminAsync(CancellationToken ct = default) => Task.FromResult(true);

  public Task<bool> AuthorizeStravaAsync(CancellationToken ct = default) => Task.FromResult(true);
  public Task<bool> DeauthorizeStravaAsync(CancellationToken ct = default) => Task.FromResult(true);
}
