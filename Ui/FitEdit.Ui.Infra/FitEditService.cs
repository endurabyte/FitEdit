using FitEdit.Model;
using FitEdit.Model.Clients;
using FitEdit.Model.Data;
using FitEdit.Model.GarminConnect;
using FitEdit.Services;
using FitEdit.Ui.Model.Supabase;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.Infra;

public class FitEditService : ReactiveObject, IFitEditService
{
  [Reactive] public bool IsAuthenticated { get; private set; }
  [Reactive] public bool IsAuthenticating { get; private set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }
  [Reactive] public bool IsAuthenticatingWithGarmin { get; private set; }
  [Reactive] public bool IsAuthenticatedWithStrava { get; private set; }
  [Reactive] public bool IsAuthenticatingWithStrava { get; private set; }
  [Reactive] public bool IsActive { get; private set; }
  [Reactive] public bool SupportsPayments { get; private set; }
  [Reactive] public string? Username { get; set; }
  [Reactive] public List<GarminCookie> GarminCookies { get; set; } = new();
  [Reactive] public DateTime LastSync { get; set; }

  private readonly ISupabaseAdapter supa_;
  private readonly IWebAuthenticator authenticator_;
  private readonly IFitEditClient client_;

  private string AccessToken_ => supa_.Authorization?.AccessToken ?? "";

  public FitEditService(ISupabaseAdapter supa, IWebAuthenticator authenticator, IFitEditClient client)
  {
    supa_ = supa;
    authenticator_ = authenticator;
    client_ = client;
    SupportsPayments = !OperatingSystem.IsIOS();

    supa_
      .ObservableForProperty(x => x.IsAuthenticatedWithGarmin)
      .Subscribe(_ =>
      {
        IsAuthenticatedWithGarmin = supa_.IsAuthenticatedWithGarmin;
        IsAuthenticatingWithGarmin = false;
      });

    supa_
      .ObservableForProperty(x => x.IsAuthenticatedWithStrava)
      .Subscribe(_ =>
      {
        IsAuthenticatedWithStrava = supa_.IsAuthenticatedWithStrava;
        IsAuthenticatingWithStrava = false;
      });

    supa_
      .ObservableForProperty(x => x.IsAuthenticated)
      .Subscribe(_ =>
      {
        IsAuthenticated = supa_.IsAuthenticated;
        IsAuthenticating = false;
      });

    supa_
      .ObservableForProperty(x => x.IsActive)
      .Subscribe(_ =>
      {
        IsActive = supa_.IsActive;
      });

    supa_
      .ObservableForProperty(x => x.Authorization)
      .Subscribe(_ =>
      {
        Username = supa_.Authorization?.Username;
        client_.AccessToken = AccessToken_;
      });

    supa_
      .ObservableForProperty(x => x.GarminCookies)
      .Subscribe(_ =>
      {
        GarminCookies = Json.MapFromJson<List<GarminCookie>>(supa_.GarminCookies ?? "") ?? new List<GarminCookie>();
      });

    supa_
      .ObservableForProperty(x => x.LastSync)
      .Subscribe(_ => LastSync = supa_.LastSync);

    this
      .ObservableForProperty(x => x.LastSync)
      .Subscribe(_ => supa_.LastSync = LastSync);

    this
      .ObservableForProperty(x => x.Username)
      .Subscribe(_ =>
      {
        authenticator_.Username = Username;
      });
  }

  public async Task Sync() => await supa_.Sync();

  public Task<bool> VerifyOtpAsync(string token) => supa_.VerifyOtpAsync(Username, token);

  public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => await client_.IsAuthenticatedAsync(ct);

  public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
  {
    authenticator_.Username = Username;
    IsAuthenticating = true;
    Log.Info($"Starting {authenticator_.GetType()}.{nameof(IWebAuthenticator.AuthenticateAsync)}");
    return await authenticator_.AuthenticateAsync(ct);
  }

  public async Task<bool> LogoutAsync(CancellationToken ct = default)
  {
    Log.Info($"Starting {authenticator_.GetType()}.{nameof(IWebAuthenticator.LogoutAsync)}");
    return await authenticator_.LogoutAsync(ct);
  }

  public async Task AuthorizeGarminAsync(CancellationToken ct)
  {
    if (!await client_.AuthorizeGarminAsync(Username, ct)) { return; }

    IsAuthenticatingWithGarmin = true;
  }

  public async Task<bool> DeauthorizeGarminAsync(CancellationToken ct = default)
  {
    if (!await client_.DeauthorizeGarminAsync(ct)) { return false; }

    IsAuthenticatingWithGarmin = true;
    return true;
  }

  public async Task<bool> AuthorizeStravaAsync(CancellationToken ct = default)
  {
    if (!await client_.AuthorizeStravaAsync(ct)) { return false; }

    IsAuthenticatingWithStrava = true;
    return true;
  }

  public async Task<bool> DeauthorizeStravaAsync(CancellationToken ct = default)
  {
    if (!await client_.DeauthorizeStravaAsync(ct)) { return false; }

    IsAuthenticatingWithStrava = true;
    return true;
  }
}
