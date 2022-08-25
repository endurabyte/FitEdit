using Dauer.Model;
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

  public Task<bool> Run()
  {
    string calendarUrl = $"https://beta.finalsurge.com/workoutcalendar";

    if (driver_.Url != calendarUrl)
    {
      if (!driver_.SignedInToFinalSurge(advise: true))
      {
        return Task.FromResult(false);
      }
    }

    if (driver_.Url != calendarUrl)
    {
      return Task.FromResult(false);
    }

    if (!search_.TryFind(Date, out IWebElement workout))
    {
      return Task.FromResult(false);
    }

    bool ok = Edit(workout, WorkoutName, Description);

    return Task.FromResult(ok);
  }

  /// <summary>
  /// Set the name or description of the given workout.
  /// </summary>
  public bool Edit(IWebElement day, string name, string description) => 
    TryOpenWorkoutModal(driver_, day, out IWebElement modal)
      && TryEditName(modal, name)
      && TryEditDescription(modal, description)
      && TrySave(driver_)
      && TryShowWorkout(driver_);

  /// <summary>
  /// Open the modal dialog by clicking the workout and then in the workout bubble clicking the edit pencil.
  /// </summary>
  private static bool TryOpenWorkoutModal(IWebDriver driver, IWebElement day, out IWebElement modal)
  {
    modal = null;

    if (!day.TryClick(By.CssSelector(".workout-item__overlay")))
    {
      Log.Error("Could not click on workout");
      return false;
    }

    if (!driver.TryClick(By.CssSelector("[id='fs-modal-content'] > div > div.content > div > div.header > div > div:nth-child(1) > div.button__border")))
    {
      Log.Error("Could not click edit button");
      return false;
    }

    if (!driver.TryFindElement(By.CssSelector("[id='fs-modal-content']"), out modal))
    {
      Log.Error("Could not find modal");
      return false;
    }

    return true;
  }

  private static bool TryEditName(IWebElement modal, string name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return true;
    }

    if (!modal.TrySetText(By.CssSelector("[placeholder=\"Workout Name\"]"), name))
    {
      Log.Error("Could not set workout name");
      return false;
    }

    return true;
  }

  private static bool TryEditDescription(IWebElement modal, string description)
  {
    if (string.IsNullOrWhiteSpace(description))
    {
      return true;
    }

    if (!modal.TrySetText(By.CssSelector("[placeholder=\"Description\"]"), description))
    {
      Log.Error("Could not set workout description");
      return false;
    }

    return true;
  }

  private static bool TrySave(IWebDriver driver)
  {
    if (!driver.TryClick(By.CssSelector("[id='fs-modal-content'] > div.content > div > div.quick-add-footer > div > div > div > button:nth-child(1)")))
    {
      Log.Error("Could not find save button");
      return false;
    }

    return true;
  }

  private static bool TryShowWorkout(IWebDriver driver)
  {
    // Wait for workout modal to close
    if (!driver.TryClick(By.CssSelector(".workout-item__overlay")))
    {
      Log.Error("Saved but could not close modal to get workout ID.");
      return false;
    }

    // Request analyzer load
    if (!driver.TryClick(By.CssSelector("[id='fs-modal-content'] > div > div.window-container")))
    {
      Log.Error("Could not find analyze button");
      return false;
    }

    // Wait for analyzer to load
    if (!driver.TryWaitForUrl(new Regex(".*workoutcalendar/workout-details/USER/")))
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
    if (!driver.TryClick(By.CssSelector("[id='fs-component_container'] > div.modal.modal--global > div.header > div.header__action > div > div.el-tooltip.button.workout-details-page__action.button--l.button--empty.button--icon-left > div.button__border")))
    {
      Log.Error("Could not close analyzer");
      return false;
    }

    return true;
  }
}