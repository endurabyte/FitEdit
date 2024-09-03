using Units;

namespace FitEdit.Model.GarminConnect;

public static class GarminActivityMapper
{
  public static (long ActivityId, LocalActivity la) MapLocalActivity(GarminActivity act)
  {
    string id = $"{Guid.NewGuid()}";
    string sourceId = $"{act.ActivityId}";
    DateTime startTime = act.GetStartTime();

    var la = new LocalActivity
    {
      Id = id,
      Source = ActivitySource.GarminConnect,
      SourceId = sourceId,
      Name = act.ActivityName,
      Description = act.Description,
      Type = act.ActivityType?.TypeKey?.ToUpper(),
      DeviceName = "unknown",
      StartTime = startTime,
      Duration = (long)(act.Duration ?? 0),
      Distance = new Quantity(act.Distance ?? 0, Unit.Meter),
      FileType = "Fit",
    };

    return (act.ActivityId, la);
  }
}
