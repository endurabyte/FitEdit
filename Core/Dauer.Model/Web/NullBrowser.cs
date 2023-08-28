#nullable enable
namespace Dauer.Model.Web;

public class NullBrowser : IBrowser
{
  public Task OpenAsync(string? url) => Task.CompletedTask;
}
