#nullable enable
using FitEdit.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.Model.Supabase;

public class NullSupabaseAdapter : ReactiveObject, ISupabaseAdapter
{
  [Reactive] public bool IsAuthenticated { get; private set; } = false;
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; } = false;
  [Reactive] public bool IsAuthenticatedWithStrava { get; private set; } = false;
  [Reactive] public bool IsActive { get; private set; } = true;
  public Authorization? Authorization { get; set; }
  public string? GarminCookies => "[{\"name\":\"GARMIN-SSO-CUST-GUID\",\"value\":\"3ebb15c3-876f-4649-8faa-1695902ce989\",\"domain\":\".garmin.com\",\"hostOnly\":false,\"path\":\"/\",\"secure\":false,\"httpOnly\":false,\"sameSite\":\"no_restriction\",\"session\":true,\"firstPartyDomain\":\"\",\"partitionKey\":null,\"storeId\":\"firefox-default\"},{\"name\":\"SESSIONID\",\"value\":\"XDViMDQ5NmItNjRlMS01NTdmLWEwYTQtYWJkNWIwYjRkYyRl\",\"domain\":\"connect.garmin.com\",\"hostOnly\":true,\"path\":\"/\",\"secure\":true,\"httpOnly\":true,\"sameSite\":\"lax\",\"session\":true,\"firstPartyDomain\":\"\",\"partitionKey\":null,\"storeId\":\"firefox-default\"}]";
  public DateTime LastSync { get; set; } = DateTime.UtcNow;

  public Task Sync()
  {
    LastSync = DateTime.UtcNow;
    return Task.CompletedTask;
  }

  public Task<bool> SignInWithEmailAndPassword(string email, string password, CancellationToken ct = default)
  {
    IsAuthenticated = true;
    IsAuthenticatedWithGarmin = true;
    IsAuthenticatedWithStrava = true;
    return Task.FromResult(true);
  }

  public Task<string?> SignInWithOtp(string username, bool usePkce, string redirectUri) => Task.FromResult(null as string);
  public Task<string?> ExchangeCodeForSession(string? verifier, string? code) => Task.FromResult(null as string);
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> VerifyOtpAsync(string? username, string token)
  {
    IsAuthenticated = true;
    IsAuthenticatedWithGarmin = true;
    IsAuthenticatedWithStrava = true;
    return Task.FromResult(true);
  }

  public Task<bool> LogoutAsync()
  {
    IsAuthenticated = false;
    IsAuthenticatedWithGarmin = false;
    IsAuthenticatedWithStrava = false;
    return Task.FromResult(true);
  }

  public Task<bool> UpdateAsync(LocalActivity? act) => Task.FromResult(false);
  public Task<bool> DeleteAsync(LocalActivity? act) => Task.FromResult(false);
}
