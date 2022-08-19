using Dauer.Model.Web;

namespace Dauer.Services;

public interface IBrowserAdapter
{
  Task<bool> Run(IStep step);
  Task Close();
}