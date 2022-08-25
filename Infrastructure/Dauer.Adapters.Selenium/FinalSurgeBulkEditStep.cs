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

  public async Task<bool> Run()
  {
    if (!await driver_.SignedInToFinalSurge(advise: true).AnyContext())
    {
      return false;
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

      IWebElement workout = await search_.TryFind(Dates[i]).AnyContext();
      if (workout == null)
      {
        Log.Error($"Could not find workout '{WorkoutNames[i]}' on {Dates[i]}");
        continue;
      }

      bool ok2 = await Resilently.RetryAsync
      (
        async () => await editor.Edit(workout, WorkoutNames[i], Descriptions[i]).AnyContext(),
        new RetryConfig
        {
          RetryLimit = 3,
          Duration = TimeSpan.FromMinutes(1),
          Description = $"Edit workout {Dates[i]} \"{WorkoutNames[i]}\""
        }
      ).AnyContext();

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

    return ok;
  }
}
