namespace Dauer.Ui;

public class NullWebAuthenticator : IWebAuthenticator
{
  public Task AuthenticateAsync() => Task.CompletedTask;
}
