using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Infra;

public abstract class WebAuthenticatorBase : ReactiveObject, IWebAuthenticator
{
  [Reactive] public string? Username { get; set; } = "";
  [Reactive] public bool LoggedIn { get; set; }

  public virtual Task<bool> AuthenticateAsync(CancellationToken ct = default) => Task.FromResult(false);
  public virtual Task<bool> LogoutAsync(CancellationToken ct = default) => Task.FromResult(false);
}
