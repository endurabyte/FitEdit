#nullable enable
namespace FitEdit.Model.Web;

public interface IBrowser
{
  Task OpenAsync(string? url);
}