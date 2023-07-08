namespace Dauer.Ui.Infra;

public interface IWebAuthenticator
{
  Task<bool> AuthenticateAsync(CancellationToken ct = default);
}
