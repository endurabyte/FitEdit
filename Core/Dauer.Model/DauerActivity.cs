#nullable enable
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Units;

namespace Dauer.Model;

public class DauerActivity : ReactiveObject
{
  /// <summary>
  /// NOT Garmin activity ID; our own independent ID
  /// </summary>
  [Reactive] public string Id { get; set; } = "";

  [Reactive] public FileReference? File { get; set; }

  public ActivitySource Source { get; set; }

  /// <summary>
  /// e.g. Garmin Activity ID
  /// </summary>
  public string SourceId { get; set; } = "";

  [Reactive] public string? Name { get; set; }
  [Reactive] public string? Description { get; set; }
  public string? Type { get; set; }
  public string? DeviceName { get; set; }
  [Reactive] public DateTime StartTime { get; set; }
  public long Duration { get; set; }
  public Quantity Distance { get; set; }
  public bool Manual { get; set; }
  public string? FileType { get; set; }
  public string? BucketUrl { get; set; }

  public DateTime? LastUpdated { get; set; }
}
