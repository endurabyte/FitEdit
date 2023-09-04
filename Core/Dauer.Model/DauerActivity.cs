#nullable enable
using System.Web;
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
  public DateTime StartTimeLocal => StartTime.ToLocalTime();
  public long Duration { get; set; }
  public Quantity Distance { get; set; }
  public bool Manual { get; set; }
  public string? FileType { get; set; }
  public string? BucketUrl { get; set; }

  public string? OnlineUrl => Source switch
  {
    ActivitySource.GarminConnect when !string.IsNullOrEmpty(SourceId) => $"https://connect.garmin.com/activity/{SourceId}",
    // When we don't have the Garmin ID, search Garmin Connect by activity name
    ActivitySource.GarminConnect => $"https://connect.garmin.com/modern/activities?startDate={StartTime:yyyy-MM-dd}&endDate={StartTime + TimeSpan.FromDays(1):yyyy-MM-dd}&search={HttpUtility.UrlEncode(Name)}",

    ActivitySource.Strava when !string.IsNullOrEmpty(SourceId) => $"https://www.strava.com/activities/{SourceId}",
    // When we don't have the Strava ID, search Strava by activity name
    ActivitySource.Strava => $"https://www.strava.com/athlete/training?keywords={Name}",
    _ => ""
  };

  public DateTime? LastUpdated { get; set; }
}
