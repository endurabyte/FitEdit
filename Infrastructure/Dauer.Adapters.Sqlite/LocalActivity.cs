#nullable enable
using SQLite;

namespace Dauer.Adapters.Sqlite;

public class LocalActivity
{
  /// <summary>
  /// NOT Garmin activity ID; our own independent ID
  /// </summary>
  [PrimaryKey]
  public string Id { get; set; } = "";

  /// <summary>
  /// Foreign key to SqliteFile
  /// </summary>
  public string? FileId { get; set; }

  public string Source { get; set; } = "Unknown";

  /// <summary>
  /// e.g. Garmin Activity ID
  /// </summary>
  public string SourceId { get; set; } = "";

  public string? Name { get; set; }
  public string? Description { get; set; }
  public string? Type { get; set; }
  public string? DeviceName { get; set; }
  public DateTime StartTime { get; set; }
  /// <summary>
  /// Unix timestamp e.g. seconds since 1970-1-1
  /// </summary>
  public long? StartTimeUnix { get; set; }
  public long Duration { get; set; }
  public double Distance { get; set; }
  public bool Manual { get; set; }
  public string? FileType { get; set; }
}
