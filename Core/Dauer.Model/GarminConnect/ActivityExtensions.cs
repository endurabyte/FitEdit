using System.Globalization;

namespace Dauer.Model.GarminConnect;

public static class ActivityExtensions
{
  public static DateTime GetStartTime(this GarminActivity act)
  {
    DateTime startTime = act.BeginTimestamp != null
      ? DateTime.UnixEpoch + TimeSpan.FromMilliseconds(act.BeginTimestamp.Value)
      : default;

    return startTime != default || DateTime.TryParseExact(act.StartTime,
             "yyyy-MM-dd HH:mm:ss",
             CultureInfo.InvariantCulture,
             DateTimeStyles.None,
             out startTime)
      ? startTime
      : default;
  }
}
