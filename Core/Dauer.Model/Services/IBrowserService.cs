using Dauer.Model.Web;

namespace Dauer.Model.Services;

public interface IBrowserService
{
  Task Run(Workflow workflow);
  Task Close();
}