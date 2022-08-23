using Dauer.Model;
using OpenQA.Selenium;
using System.Globalization;

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
  public bool GoToMonth(DateTime dt)
  {
    if (!TryGetMonthYear(out string monthYear, out IWebElement picker))
    {
      return false;
    }

    if (TryParseSafe(monthYear, out DateTime currentDt) && currentDt.AlmostEqual(dt))
    {
      // Already at the requested month/year
      return true;
    }

    // Expand the date picker
    picker.Click();

    if (!driver_.TryFindElement(By.CssSelector("[id='fs-date-picker-container']"), out IWebElement expandedPicker))
    {
      Log.Error("Could expand date picker");
      return false;
    }

    IWebElement left = expandedPicker.FindElement(By.CssSelector(".icon-arrow-left"));
    IWebElement right = expandedPicker.FindElement(By.CssSelector(".icon-arrow-right"));
    IWebElement yearElem = expandedPicker.FindElement(By.CssSelector(".calendar-picker-header__button"));

    // Select year
    while (int.TryParse(yearElem.Text, out int year) && year > dt.Year)
    {
      left.Click(); 
    }
    while (int.TryParse(yearElem.Text, out int year) && year < dt.Year)
    {
      right.Click();
    }

    // Wait for slide animation to stop
    Thread.Sleep(500);

    // After changing the year, any previous cell reference is now stale
    IReadOnlyCollection<IWebElement> cells = expandedPicker.FindElements(By.CssSelector(".date-picker-view__cell"));

    // Select month
    IWebElement cell = cells.FirstOrDefault(cell => cell.Text.Contains($"{dt:MMM}")); // e.g. "Aug", "Jul"

    if (cell == null)
    {
      Log.Error("Could not set month");
      return false;
    }

    cell?.Click(); // Closes the picker. 

    return true;
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

    // Days without just have the day of the month e.g. "1" or "28"
    int minLen = 2;

    List<IWebElement> days = driver_
      .FindElements(By.CssSelector(".fs-calendar-week-day-view"))
        .Where(elem => elem.Text.Length > minLen)
      .ToList();

    var dict = new Dictionary<DateTime, IWebElement>();

    foreach (IWebElement day in days)
    {
      string date = KeyFor(day, monthYear);

      if (!TryParseSafe(date, out DateTime key))
      {
        continue;
      }

      dict[key] = day;
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

  private static string KeyFor(IWebElement day, string monthYear)
  {
    if (!day.TryFindElement(By.CssSelector(".fs-cwdv-date"), out IWebElement daySpan))
    {
      // Not a calendar day
      return "";
    }

    if (!day.TryFindElement(By.CssSelector(".workout-item__time > span"), out IWebElement timeSpan))
    {
      // No workout that day
      return "";
    }

    // Format is e.g. "08:09 8 August 2022"
    return $"{timeSpan.Text} {daySpan.Text} {monthYear}";
  }

  private bool TryParseSafe(string date, out DateTime dt)
  {
    try
    {
      return DateTime.TryParseExact(date, format_, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }
    catch (Exception)
    {
      dt = default;
      return false;
    }
  }
}
