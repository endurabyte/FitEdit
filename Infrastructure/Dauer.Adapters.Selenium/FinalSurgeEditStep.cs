using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Web;
using OpenQA.Selenium;
using System.Text.RegularExpressions;

namespace Dauer.Adapters.Selenium;

public class FinalSurgeEditStep : Step, IStep
{
  private readonly FinalSurgeCalendarSearch search_;

  public DateTime Date { get; set; }
  public string WorkoutName { get; set; }
  public string Description { get; set; }

  public FinalSurgeEditStep(IWebDriver driver, FinalSurgeCalendarSearch search) : base(driver)
  {
    Name = "Final Surge Edit";
    search_ = search;
  }

  public async Task<bool> Run()
  {
    string calendarUrl = $"https://beta.finalsurge.com/workoutcalendar";

    if (driver_.Url != calendarUrl)
    {
      if (!await driver_.SignedInToFinalSurge(advise: true).AnyContext())
      {
        return false;
      }
    }

    if (driver_.Url != calendarUrl)
    {
      return false;
    }

    IWebElement workout = await search_.TryFind(Date).AnyContext();
    if (workout == null)
    {
      return false;
    }

    bool ok = await Edit(workout, WorkoutName, Description).AnyContext();

    return ok;
  }

  /// <summary>
  /// Set the name or description of the given workout.
  /// </summary>
  public async Task<bool> Edit(IWebElement day, string name, string description)
  {
   IWebElement modal = await TryOpenQuickEditModal(driver_, day, Date).AnyContext();

    return modal != null
      && await TryEditName(modal, name).AnyContext()
      && await TryEditDescription(modal, description).AnyContext()
      && await TrySave(driver_).AnyContext()
      && await TryShowWorkout(driver_).AnyContext();
  }

  /// <summary>
  /// Open the workout quick edit modal by clicking the workout and then in the workout bubble clicking the edit pencil.
  /// </summary>
  private static async Task<IWebElement> TryOpenQuickEditModal(IWebDriver driver, IWebElement day, DateTime date)
  {
    List<IWebElement> workouts = day.FindElements(By.CssSelector(".fs-workout-item")).ToList();

    IWebElement workout = workouts.FirstOrDefault(wc =>
    {
      if (!wc.TryFindElement(By.CssSelector(".workout-item__time > span"), out IWebElement time))
      {
        Log.Error("Could not find workout time");
        return false;
      }

      if (!DateTimeFactory.TryParseSafe(time.Text, out DateTime dt, "HH:mm"))
      {
        Log.Error("Could not parse workout time");
      }

      return dt.Hour == date.Hour && dt.Minute == date.Minute;
    });

    // Opens the workout modal
    if (!await workout.TryClick().AnyContext())
    {
      Log.Error("Could not click on workout");
      return null;
    }

    if (!day.TryFindElement(By.CssSelector(".workout-modal-container"), out IWebElement modal))
    {
      Log.Error("Could not click on workout");
      return null;
    }

    if (!modal
      .TryClick(By.CssSelector(".fs-responsive-modal > [id='fs-modal-content'] > div > div.content > div.fs-workout-preview > div.header > div.button-group > div.button"))
      .Await())
    {
      Log.Error("Could not click edit button");
      return null;
    }

    if (!driver.TryFindElement(By.CssSelector("[id='fs-modal-content']"), out IWebElement quickEditModal))
    {
      Log.Error("Could not find modal");
      return null;
    }

    return quickEditModal;
  }

  private static async Task<bool> TryEditName(IWebElement modal, string name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return true;
    }

    if (!await modal.TrySetText(By.CssSelector("[placeholder=\"Workout Name\"]"), name).AnyContext())
    {
      Log.Error("Could not set workout name");
      return false;
    }

    return true;
  }

  private static async Task<bool> TryEditDescription(IWebElement modal, string description)
  {
    if (string.IsNullOrWhiteSpace(description))
    {
      return true;
    }

    if (!await modal.TrySetText(By.CssSelector("[placeholder=\"Description\"]"), description).AnyContext())
    {
      Log.Error("Could not set workout description");
      return false;
    }

    return true;
  }

  private static async Task<bool> TrySave(IWebDriver driver)
  {
    if (!await driver
      .TryClick(By.CssSelector("[id='fs-modal-content'] > div.content > div > div.quick-add-footer > div > div > div > button:nth-child(1)"))
      .AnyContext())
    {
      Log.Error("Could not find save button");
      return false;
    }

    return true;
  }

  private static async Task<bool> TryShowWorkout(IWebDriver driver)
  {
    // Wait for workout modal to close
    if (!await driver.TryClick(By.CssSelector(".workout-item__overlay")).AnyContext())
    {
      Log.Error("Saved but could not close modal to get workout ID.");
      return false;
    }

    // Request analyzer load
    if (!await driver.TryClick(By.CssSelector("[id='fs-modal-content'] > div > div.window-container")).AnyContext())
    {
      Log.Error("Could not find analyze button");
      return false;
    }

    // Wait for analyzer to load
    if (!await driver.TryWaitForUrl(new Regex(".*workoutcalendar/workout-details/USER/")).AnyContext())
    {
      Log.Error("Could not open analyzer");
      return false;
    }

    string url = driver.Url;
    string[] split = url.Split('/');
    string userId = split[split.Length - 2];
    string workoutId = split[split.Length - 1];

    Log.Debug($"Workout ID: {workoutId}");
    Log.Debug($"URL: {driver.Url}");

    // Close analyzer
    if (!await driver
      .TryClick(By.CssSelector("[id='fs-component_container'] > div.modal.modal--global > div.header > div.header__action > div > div.el-tooltip.button.workout-details-page__action.button--l.button--empty.button--icon-left > div.button__border"))
      .AnyContext())
    {
      Log.Error("Could not close analyzer");
      return false;
    }

    return true;
  }
}