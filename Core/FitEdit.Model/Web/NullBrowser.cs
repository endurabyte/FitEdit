#nullable enable
namespace FitEdit.Model.Web;

public class NullBrowser : IBrowser
{
  public Task OpenAsync(string? url) => Task.CompletedTask;
}
