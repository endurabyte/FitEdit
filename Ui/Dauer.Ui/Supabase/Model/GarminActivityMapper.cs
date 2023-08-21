using Dauer.Model;
using Units;

namespace Dauer.Ui.Supabase.Model;

public static class GarminActivityMapper
{
  public static DauerActivity MapDauerActivity(this GarminActivity a) => new()
  {
    Id = a.Id,
    Source = ActivitySource.GarminConnect,
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
  };
}