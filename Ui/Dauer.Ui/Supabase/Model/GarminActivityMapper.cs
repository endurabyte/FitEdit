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
    StartTime = new Dynastream.Fit.DateTime((uint)a.StartTime).GetDateTime(),
    Duration = a.Duration,
    Distance = new Quantity(a.Distance, Unit.Meter),
    Manual = a.Manual,
    FileType = a.FileType,
  };
}