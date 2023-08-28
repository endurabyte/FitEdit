#nullable enable
namespace Dauer.Model.Web;

public interface IBrowser
{
  Task OpenAsync(string? url);
}