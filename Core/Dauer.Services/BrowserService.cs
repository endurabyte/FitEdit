using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Web;

namespace Dauer.Services;

public class BrowserService : IBrowserService
{
  private readonly IBrowserAdapter adapter_;

  public BrowserService(IBrowserAdapter adapter)
  {
    adapter_ = adapter;
  }

  public async Task Run(Workflow workflow) => await Task.Run(async () =>
  {
    Log.Info($"Running workflow {workflow.Name}");

    try
    {
      foreach (IStep step in workflow)
      {
        Log.Info($"Running step {step.Name}");

        if (!await adapter_.Run(step).AnyContext())
        {
          break;
        }
      }
    }
    catch (Exception e)
    {
      Log.Error(e);
    }

  }).AnyContext();

  public async Task Close() => await adapter_.Close().AnyContext();

}