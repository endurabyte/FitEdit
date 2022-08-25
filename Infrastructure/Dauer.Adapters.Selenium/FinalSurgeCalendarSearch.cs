using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;

namespace Dauer.Adapters.Selenium;

public class FinalSurgeCalendarSearch
{
  private readonly FinalSurgeCalendar calendar_;


  public FinalSurgeCalendarSearch(FinalSurgeCalendar calendar)
  {
    calendar_ = calendar;
  }

  /// <summary>
  /// Find the <see cref="IWebElement"/> for the workout which starts at the given <see cref="DateTime"/>.
  /// </summary>
  public bool TryFind(DateTime dt, out IWebElement workout) => FindAll(new[] { dt }).TryGetValue(dt, out workout);

  /// <summary>
  /// Find the <see cref="IWebElement"/> for each workout which starts at each given <see cref="DateTime"/>.
  /// </summary>
  public Dictionary<DateTime, IWebElement> FindAll(IEnumerable<DateTime> dts)
  {
    var dtsSorted = dts.ToList();
    dtsSorted.Sort(); // Oldest to newest

    Dictionary<DateTime, IWebElement> source = null;
    Dictionary<DateTime, IWebElement> dest = new();

    DateTime lastDt = default;
    foreach (DateTime dt in dtsSorted)
    {
      if (lastDt == default || dt.Month != lastDt.Month)
      {
        bool didSetMonth = Resilently.RetryAsync
        (
          () => Task.FromResult(calendar_.GoToMonth(dt)),
          
          new RetryConfig 
          { 
            RetryLimit = 3, 
            Duration = TimeSpan.FromMinutes(1),
            Description = "Set month/year"
          }
        ).Await();

        if (!didSetMonth)
        {
          continue;
        }

        source = calendar_.ReadMonth();
      }

      if (TryMatch(dt, source.Keys, out DateTime match)
        && source.TryGetValue(match, out IWebElement workout))
      {
        dest[dt] = workout;
      }

      lastDt = dt;
    }

    return dest;
  }

  /// <summary>
  /// Find the first <see cref="DateTime"/> that is equal to the given <see cref="DateTime"/>.
  /// Return false iff there is no such match.
  /// </summary>
  private bool TryMatch(DateTime dt, IEnumerable<DateTime> all, out DateTime match)
  {
    match = all.FirstOrDefault(dt2 =>
    {
      try
      {
        return dt.AlmostEqual(dt2);
      }
      catch (Exception)
      {
        return false;
      }
    });

    return match != default;
  }

}

public static class DateTimeExtensions
{
  /// <summary>
  /// By default, dates are considered equal if they are within this duration of each other.
  /// </summary>
  public static TimeSpan Precision { get; set; } = TimeSpan.FromMinutes(1);

  /// <summary>
  /// Return true iff the given <see cref="DateTime"/>s are within <see cref="Precision"/>.
  /// </summary>
  public static bool AlmostEqual(this DateTime dt, DateTime dt2) => Math.Abs((dt2 - dt).TotalMinutes) < Precision.TotalMinutes;
}
