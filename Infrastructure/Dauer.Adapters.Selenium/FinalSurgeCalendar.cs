using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public class FinalSurgeCalendar
{
  /// <summary>
  /// Format is e.g. "08:09 8 August 2022"
  /// </summary>
  private readonly string format_ = "HH:mm d MMMM yyyy";
  private readonly IWebDriver driver_;

  public FinalSurgeCalendar(IWebDriver driver)
  {
    driver_ = driver;
  }

  /// <summary>
  /// Navigate the date picker to the given month
  /// </summary>
  public async Task<bool> GoToMonth(DateTime dt)
  {
    if (!TryGetMonthYear(out string monthYear, out IWebElement picker))
    {
      return false;
    }

    var dtMonth = new DateTime(dt.Year, dt.Month, 1);
    if (DateTimeFactory.TryParseSafe(monthYear, out DateTime currentDt, "MMMM yyyy") && currentDt.AlmostEqual(dtMonth))
    {
      // Already at the requested month/year
      return true;
    }

    // Expand the date picker
    await picker.TryClick().AnyContext();

    if (!driver_.TryFindElement(By.CssSelector("[id='fs-date-picker-container']"), out IWebElement expandedPicker))
    {
      Log.Error("Could not expand date picker");
      return false;
    }

    IWebElement left = expandedPicker.FindElement(By.CssSelector(".icon-arrow-left"));
    IWebElement right = expandedPicker.FindElement(By.CssSelector(".icon-arrow-right"));
    IWebElement year = expandedPicker.FindElement(By.CssSelector(".calendar-picker-header__button"));

    // Select year
    while (int.TryParse(year.Text, out int yearInt) && yearInt > dt.Year)
    {
      await left.TryClick().AnyContext();
    }
    while (int.TryParse(year.Text, out int yearInt) && yearInt < dt.Year)
    {
      await right.TryClick().AnyContext();
    }

    // Select month
    // Wait for slide animation to finish, otherwise cells are for the year currently sliding through
    Thread.Sleep(500);

    // After changing the year, any previous cell reference is now stale
    IReadOnlyCollection<IWebElement> cells = expandedPicker.FindElements(By.CssSelector(".date-picker-view__cell"));
    IWebElement cell = cells.FirstOrDefault(cell => cell.Text.Contains($"{dt:MMM}")); // e.g. "Aug", "Jul"

    bool didSetMonth = cell switch
    {
      null => false,
      _ => await cell.TryClick().AnyContext()
    };

    if (!didSetMonth)
    {
      Log.Error("Could not set year and/or month");
    }

    return didSetMonth;
  }

  /// <summary>
  /// Get all workouts on the current calendar month
  /// </summary>
  public Dictionary<DateTime, IWebElement> ReadMonth()
  {
    if (!TryGetMonthYear(out string monthYear, out _))
    {
      return null;
    }

    // Days without workouts just have the day of the month e.g. "1" or "28"
    int minLen = 2;

    List<IWebElement> days = driver_
      .FindElements(By.CssSelector(".fs-calendar-week-day-view"))
        .Where(elem => elem.Text.Length > minLen)
      .ToList();

    var dict = new Dictionary<DateTime, IWebElement>();

    foreach (IWebElement day in days)
    {
      List<string> dates = KeysFor(day, monthYear);

      foreach (string date in dates)
      {
        if (!DateTimeFactory.TryParseSafe(date, out DateTime key, format_))
        {
          continue;
        }

        dict[key] = day;
      }
    }

    return dict;
  }

  /// <summary>
  /// Return the text value of the unexpanded date picker e.g. "August 2022"
  /// </summary>
  private bool TryGetMonthYear(out string monthYear, out IWebElement picker)
  {
    if (!driver_.TryFindElement(By.CssSelector("span[data-v-80090f7a]"), out picker))
    {
      Log.Error("Could not find date picker");
      monthYear = null;
      return false;
    }

    monthYear = picker.Text;
    return true;
  }

  private static List<string> KeysFor(IWebElement day, string monthYear)
  {
    if (!day.TryFindElement(By.CssSelector(".fs-cwdv-date"), out IWebElement daySpan))
    {
      // Not a calendar day
      return new List<string>();
    }

    // empty => No workout that day
    List<IWebElement> times = day.FindElements(By.CssSelector(".workout-item__time > span")).ToList();

    // Format is e.g. "08:09 8 August 2022"
    return times.Select(time => $"{time.Text} {daySpan.Text} {monthYear}").ToList();
  }
}
