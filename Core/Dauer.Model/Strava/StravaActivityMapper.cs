#nullable enable
using Units;

namespace Dauer.Model.Strava;

public static class StravaActivityMapper
{
  public static (long id, LocalActivity la) MapLocalActivity(StravaActivity act)
  {
    string id = $"{Guid.NewGuid()}";
    string sourceId = $"{act.Id}";
    DateTime startTime = act.GetStartTime();

    var la = new LocalActivity
    {
      Id = id,
      Source = ActivitySource.GarminConnect,
      SourceId = sourceId,
      Name = act.Name,
      Description = act.Description,
      Type = act.Type,
      DeviceName = "unknown",
      StartTime = startTime,
      Duration = (long)(act.ElapsedTimeRaw),
      Distance = new Quantity(act.DistanceRaw, Unit.Meter),
      FileType = "Fit",
    };

    return (act.Id, la);
  }
}
