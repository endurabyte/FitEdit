using System.Globalization;

namespace Dauer.Model.Factories;

public static class DateTimeFactory
{
  public static bool TryParseSafe(string date, out DateTime dt, string format)
  {
    try
    {
      return DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }
    catch (Exception)
    {
      dt = default;
      return false;
    }
  }
}
