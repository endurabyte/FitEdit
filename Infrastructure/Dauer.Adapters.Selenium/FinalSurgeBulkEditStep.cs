using Dauer.Model;
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

    //var workouts = search_.FindAll(Dates);

    bool ok = true;
    foreach (int i in Enumerable.Range(0, Dates.Count))
    {
      var editor = new FinalSurgeEditStep(driver_, search_)
      {
        Date = Dates[i],
        WorkoutName = WorkoutNames[i],
        Description = Descriptions[i],
      };

      if (!search_.TryFind(Dates[i], out IWebElement workout))
      {
        Log.Error($"Could not find workout '{WorkoutNames[i]}' on {Dates[i]}");
        continue;
      }

      ok &= editor.Edit(workout, WorkoutNames[i], Descriptions[i]);
    }

    return Task.FromResult(ok);
  }
}
