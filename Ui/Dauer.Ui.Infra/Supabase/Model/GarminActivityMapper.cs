using Dauer.Model;
using Units;

namespace Dauer.Ui.Infra.Supabase.Model;

public static class GarminActivityMapper
{
  public static DauerActivity MapDauerActivity(this GarminActivity a) => new()
  {
    Id = a.Id,
    Source = ActivitySource.GarminConnect,
    SourceId = a.GarminId <= 0 ? "" : $"{a.GarminId}",
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

  public static GarminActivity MapGarminActivity(this DauerActivity a) => new()
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
    GarminId = int.TryParse(a.SourceId, out int garminId) ? garminId : 0,
    LastUpdated = a.LastUpdated,
  };
}