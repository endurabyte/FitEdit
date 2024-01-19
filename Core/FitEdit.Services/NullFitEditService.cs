#nullable enable
using FitEdit.Model.GarminConnect;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Services;

public class NullFitEditService : ReactiveObject, IFitEditService
{
  [Reactive] public bool IsAuthenticated { get; private set; } = true;
  [Reactive] public bool IsAuthenticating { get; private set; } = true;
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; } = true;
  [Reactive] public bool IsAuthenticatingWithGarmin { get; private set; } = false;
  [Reactive] public bool IsAuthenticatedWithStrava { get; private set; } = true;
  [Reactive] public bool IsAuthenticatingWithStrava { get; private set; } = false;
  [Reactive] public bool IsActive { get; set; } = true;
  [Reactive] public bool SupportsPayments { get; set; } = true;
  [Reactive] public string? Username { get; set; } = "fake@fake.com";
  [Reactive] public List<GarminCookie> GarminCookies { get; set; } = new();
  public DateTime LastSync { get; set; } = DateTime.UtcNow;

  public Task Sync()
  {
    LastSync = DateTime.UtcNow;
    return Task.CompletedTask;
  }

  public Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    IsAuthenticating = true;
    return Task.FromResult(false);
  }

  public Task<bool> LogoutAsync(CancellationToken ct = default)
  {
    IsAuthenticated = false;
    return Task.FromResult(true);
  }

  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(IsAuthenticated);
  public Task<bool> VerifyOtpAsync(string token)
  {
    IsAuthenticating = false;
    IsAuthenticated = true;
    return Task.FromResult(true);
  }

  public Task AuthorizeGarminAsync(CancellationToken ct = default)
  {
    IsAuthenticatingWithGarmin = true;
    _ = Task.Run(async () =>
    {
      await Task.Delay(1000, ct);
      IsAuthenticatingWithGarmin = false;
    }, ct);
    IsAuthenticatedWithGarmin = true;
    return Task.CompletedTask;
  }

  public Task<bool> DeauthorizeGarminAsync(CancellationToken ct = default)
  {
    IsAuthenticatedWithGarmin = false;
    return Task.FromResult(true);
  }

  public async Task<bool> AuthorizeStravaAsync(CancellationToken ct = default)
  {
    IsAuthenticatingWithStrava = true;
    await Task.Delay(1000, ct);
    IsAuthenticatingWithStrava = false;
    IsAuthenticatedWithStrava = true;
    return true;
  }

  public Task<bool> DeauthorizeStravaAsync(CancellationToken ct = default)
  {
    IsAuthenticatedWithStrava = false;
    return Task.FromResult(true);
  }
}
