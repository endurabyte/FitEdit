using Dauer.Model.Web;

namespace Dauer.Services;

public interface IBrowserService
{
  Task Run(Workflow workflow);
  Task Close();
}