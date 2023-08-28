using Dauer.Model;
using Dauer.Model.Clients;
using Dauer.Services;
using Dauer.Ui.Model.Supabase;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Infra;

public class FitEditService : ReactiveObject, IFitEditService
{
  [Reactive] public bool IsAuthenticated { get; private set; }
  [Reactive] public bool IsAuthenticating { get; private set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }
  [Reactive] public bool IsAuthenticatingWithGarmin { get; private set; }
  [Reactive] public bool IsAuthenticatedWithStrava { get; private set; }
  [Reactive] public bool IsAuthenticatingWithStrava { get; private set; }
  [Reactive] public bool IsActive { get; private set; }
  [Reactive] public string? Username { get; set; }

  private readonly ISupabaseAdapter supa_;
  private readonly IWebAuthenticator authenticator_;
  private readonly IFitEditClient client_;

  private string AccessToken_ => supa_.Authorization?.AccessToken ?? "";

  public FitEditService(ISupabaseAdapter supa, IWebAuthenticator authenticator, IFitEditClient client)
  {
    supa_ = supa;
    authenticator_ = authenticator;
    client_ = client;

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

    this
      .ObservableForProperty(x => x.Username)
      .Subscribe(_ =>
      {
        authenticator_.Username = Username;
      });
  }

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
    if (!await client_.DeauthorizeGarminAsync(Username, ct)) { return false; }

    IsAuthenticatingWithGarmin = true;
    return true;
  }

  public async Task<bool> AuthorizeStravaAsync(CancellationToken ct = default)
  {
    if (!await client_.AuthorizeStravaAsync(Username, ct)) { return false; }

    IsAuthenticatingWithStrava = true;
    return true;
  }

  public async Task<bool> DeauthorizeStravaAsync(CancellationToken ct = default)
  {
    if (!await client_.DeauthorizeStravaAsync(Username, ct)) { return false; }

    IsAuthenticatingWithStrava = true;
    return true;
  }
}
