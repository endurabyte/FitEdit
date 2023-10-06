using Dauer.Model;
using Dauer.Model.Extensions;
using Units;

namespace Dauer.Ui.Infra.Supabase.Model;

public static class ActivityMapper
{
  public static LocalActivity MapLocalActivity(this Activity a) => new()
  {
    Id = a.Id,
    Source = a.Source?.Map<ActivitySource>() ?? ActivitySource.Unknown,
    SourceId = a.SourceId <= 0 ? "" : $"{a.SourceId}",
    Name = a.Name,
    Description = a.Description,
    Type = a.Type,
    DeviceName = a.DeviceName,
    StartTime = DateTime.UnixEpoch + TimeSpan.FromSeconds(a.StartTime),
    Duration = a.Duration,
    Distance = new Quantity(a.Distance, Unit.Meter),
    Manual = a.Manual,
    FileType = a.FileType,
    BucketUrl = a.BucketUrl,
    LastUpdated = a.LastUpdated,
  };

  public static Activity MapActivity(this LocalActivity a) => new()
  {
    Id = a.Id,
    Name = a.Name,
    Description = a.Description,
    Type = a.Type,
    DeviceName = a.DeviceName,
    StartTime = (long)(a.StartTime - DateTime.UnixEpoch).TotalSeconds,
    Duration = a.Duration,
    Distance = a.Distance.Meters(),
    Manual = a.Manual,
    FileType = a.FileType,
    BucketUrl = a.BucketUrl,
    SourceId = long.TryParse(a.SourceId, out long garminId) ? garminId : 0,
    Source = $"{a.Source}",
    LastUpdated = a.LastUpdated,
  };
}