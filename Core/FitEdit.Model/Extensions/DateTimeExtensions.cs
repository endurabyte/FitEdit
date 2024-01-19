namespace FitEdit.Model.Extensions;

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

  /// <summary>
  /// Gets the unix timestamp.
  /// </summary>
  /// <param name="date">The date.</param>
  /// <returns></returns>
  public static long GetUnixTimestamp(this DateTime date) => (int)date.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
}
