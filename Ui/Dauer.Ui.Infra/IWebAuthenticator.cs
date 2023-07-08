namespace Dauer.Ui.Infra;

public interface IWebAuthenticator
{
  string? Username { get; }
  bool LoggedIn { get; set; }

  Task<bool> AuthenticateAsync(CancellationToken ct = default);
  Task<bool> LogoutAsync(CancellationToken ct = default);
}