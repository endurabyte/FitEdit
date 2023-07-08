namespace Dauer.Ui.Infra;

public class NullWebAuthenticator : IWebAuthenticator
{
  public Task<bool> AuthenticateAsync(CancellationToken ct = default) => Task.FromResult(true);
}
