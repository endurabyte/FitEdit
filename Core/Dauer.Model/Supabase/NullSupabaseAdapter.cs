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
  public string? GarminCookies => "[{\"name\":\"GARMIN-SSO-CUST-GUID\",\"value\":\"3ebb15c3-876f-4649-8faa-1695902ce989\",\"domain\":\".garmin.com\",\"hostOnly\":false,\"path\":\"/\",\"secure\":false,\"httpOnly\":false,\"sameSite\":\"no_restriction\",\"session\":true,\"firstPartyDomain\":\"\",\"partitionKey\":null,\"storeId\":\"firefox-default\"},{\"name\":\"SESSIONID\",\"value\":\"XDViMDQ5NmItNjRlMS01NTdmLWEwYTQtYWJkNWIwYjRkYyRl\",\"domain\":\"connect.garmin.com\",\"hostOnly\":true,\"path\":\"/\",\"secure\":true,\"httpOnly\":true,\"sameSite\":\"lax\",\"session\":true,\"firstPartyDomain\":\"\",\"partitionKey\":null,\"storeId\":\"firefox-default\"}]";

  public Task<bool> SignInWithEmailAndPassword(string email, string password, CancellationToken ct = default) => Task.FromResult(false);
  public Task<string?> SignInWithOtp(string username, bool usePkce, string redirectUri) => Task.FromResult(null as string);
  public Task<string?> ExchangeCodeForSession(string? verifier, string? code) => Task.FromResult(null as string);
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> VerifyOtpAsync(string? username, string token) => Task.FromResult(false);
  public Task<bool> LogoutAsync() => Task.FromResult(false);

  public Task<bool> UpdateAsync(DauerActivity? act) => Task.FromResult(false);
  public Task<bool> DeleteAsync(DauerActivity? act) => Task.FromResult(false);
}
