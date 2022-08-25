using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Web;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public class FinalSurgeBulkEditStep : Step, IStep
{
  private readonly FinalSurgeCalendarSearch search_;
  public List<DateTime> Dates { get; set; }
  public List<string> WorkoutNames { get; set; }
  public List<string> Descriptions { get; set; }

  public FinalSurgeBulkEditStep(IWebDriver driver, FinalSurgeCalendarSearch search) : base(driver)
  {
    Name = "Final Surge Bulk Edit";
    search_ = search;
  }

  public Task<bool> Run()
  {
    if (!driver_.SignedInToFinalSurge(advise: true))
    {
      return Task.FromResult(false);
    }

    bool ok = true;
    foreach (int i in Enumerable.Range(0, Dates.Count))
    {
      var editor = new FinalSurgeEditStep(driver_, search_)
      {
        Date = Dates[i],
        WorkoutName = WorkoutNames[i],
        Description = Descriptions[i],
      };

      Log.Info($"Editing {Dates[i]} \"{WorkoutNames[i]}\"");

      if (!search_.TryFind(Dates[i], out IWebElement workout))
      {
        Log.Error($"Could not find workout '{WorkoutNames[i]}' on {Dates[i]}");
        continue;
      }

      bool ok2 = Resilently.RetryAsync
      (
        () => Task.FromResult(editor.Edit(workout, WorkoutNames[i], Descriptions[i])), 
        new RetryConfig
        {
          RetryLimit = 3,
          Duration = TimeSpan.FromMinutes(1),
          Description = $"Edit workout {Dates[i]} \"{WorkoutNames[i]}\""
        }
      ).Await();

      if (ok2)
      {
        Log.Info($"Edit OK");
      }
      else
      {
        Log.Error($"Edit Error");
      }
      ok &= ok2;
    }

    return Task.FromResult(ok);
  }
}
