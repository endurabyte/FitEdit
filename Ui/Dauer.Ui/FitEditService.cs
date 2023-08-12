using System.Text.Json;
using System.Web;
using Dauer.Model;
using Dauer.Ui.Infra;
using Dauer.Ui.Supabase;
using IdentityModel.Client;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui;

public interface IFitEditService
{
  bool IsAuthenticated { get; }
  bool IsAuthenticating { get; }
  bool IsAuthenticatingWithGarmin { get; }
  bool IsAuthenticatedWithGarmin { get; }
  string? Username { get; set; }

  Task<bool> AuthenticateAsync(CancellationToken ct = default);
  Task<bool> LogoutAsync(CancellationToken ct = default);
  Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
  Task AuthorizeGarminAsync(CancellationToken ct = default);
  Task<bool> VerifyEmailAsync(string token);
  Task<bool> DeauthorizeGarminAsync(CancellationToken ct = default);
}

public class NullFitEditService : IFitEditService
{
  public bool IsAuthenticated => false;
  public bool IsAuthenticating => false;
  public bool IsAuthenticatedWithGarmin => false;
  public bool IsAuthenticatingWithGarmin => false;
  public string? Username { get; set; } = "fake@fake.com";

  public Task<bool> AuthenticateAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> LogoutAsync(CancellationToken ct = default) => Task.FromResult(false);
  public Task<bool> IsAuthenticatedAsync(CancellationToken ct = default) => Task.FromResult(true);
  public Task AuthorizeGarminAsync(CancellationToken ct = default) => Task.CompletedTask;
  public Task<bool> VerifyEmailAsync(string token) => Task.FromResult(true);
  public Task<bool> DeauthorizeGarminAsync(CancellationToken ct = default) => Task.FromResult(true);
}

public class FitEditService : ReactiveObject, IFitEditService
{
  [Reactive] public bool IsAuthenticated { get; private set; }
  [Reactive] public bool IsAuthenticating { get; private set; }
  [Reactive] public bool IsAuthenticatedWithGarmin { get; private set; }
  [Reactive] public bool IsAuthenticatingWithGarmin { get; private set; }
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
      .ObservableForProperty(x => x.IsAuthenticated)
      .Subscribe(_ =>
      {
        IsAuthenticated = supa_.IsAuthenticated;
        IsAuthenticating = false;
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

  public Task<bool> VerifyEmailAsync(string token) => supa_.VerifyEmailAsync(Username, token);

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
}
