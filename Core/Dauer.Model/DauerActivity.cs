#nullable enable
using Units;

namespace Dauer.Model;

public class DauerActivity
{
  /// <summary>
  /// NOT Garmin activity ID; our own independent ID
  /// </summary>
  public string Id { get; set; } = "";

  public BlobFile? File { get; set; }

  public ActivitySource Source { get; set; }

  /// <summary>
  /// e.g. Garmin Activity ID
  /// </summary>
  public string SourceId { get; set; } = "";

  public string? Name { get; set; }
  public string? Description { get; set; }
  public string? Type { get; set; }
  public string? DeviceName { get; set; }
  public DateTime StartTime { get; set; }
  public long Duration { get; set; }
  public Quantity Distance { get; set; }
  public bool Manual { get; set; }
  public string? FileType { get; set; }
  public string? BucketUrl { get; set; }
}
