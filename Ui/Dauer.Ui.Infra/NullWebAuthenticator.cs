namespace Dauer.Ui.Infra;

public class NullWebAuthenticator : IWebAuthenticator
{
  public Task AuthenticateAsync() => Task.CompletedTask;
}
