#nullable enable

namespace FitEdit.Model.Strava;

public static class StravaActivityExtensions
{
  public static DateTime GetStartTime(this StravaActivity act) => DateTime.TryParse(act.StartTime, out var startTime) 
    ? startTime.ToUniversalTime() 
    : default;
}