#nullable enable
using Units;
using Dauer.Model.Extensions;

namespace Dauer.Adapters.Sqlite;

public static class LocalActivityMapper
{
  public static Model.LocalActivity MapModel(this LocalActivity a) => new()
  {
    Id = a.Id,
    Source = a.Source?.Map<Model.ActivitySource>() ?? Model.ActivitySource.Unknown,
    SourceId = a.SourceId,
    Name = a.Name,
    Description = a.Description,
    Type = a.Type,
    DeviceName = a.DeviceName,
    StartTime  = a.StartTime,
    Duration = a.Duration,
    Distance = new Quantity(a.Distance, Unit.Meter),
    Manual = a.Manual,
    FileType = a.FileType,
  };

  public static LocalActivity MapEntity(this Model.LocalActivity a) => new()
  {
    Id = a.Id,
    FileId = a.File?.Id,
    Source = $"{a.Source}",
    SourceId = a.SourceId,
    Name = a.Name,
    Description = a.Description,
    Type = a.Type,
    DeviceName = a.DeviceName,
    StartTime = a.StartTime,
    StartTimeUnix = a.StartTime.GetUnixTimestamp(),
    Duration = a.Duration,
    Distance = a.Distance.Meters(),
    Manual = a.Manual,
    FileType = a.FileType,
  };
}
