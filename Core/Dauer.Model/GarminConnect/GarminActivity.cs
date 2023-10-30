using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

/// <summary>
/// Activity
/// </summary>
public class GarminActivity
{
  /// <summary>
  /// Gets or sets the activity identifier.
  /// </summary>
  /// <value>
  /// The activity identifier.
  /// </value>
  [JsonPropertyName("activityId")]
  public long ActivityId { get; set; }

  /// <summary>
  /// Gets or sets the name of the activity.
  /// </summary>
  /// <value>
  /// The name of the activity.
  /// </value>
  [JsonPropertyName("activityName")]
  public string ActivityName { get; set; }

  /// <summary>
  /// Gets or sets the description.
  /// </summary>
  /// <value>
  /// The description.
  /// </value>
  [JsonPropertyName("description")]
  public string Description { get; set; }

  /// <summary>
  /// Format: yyyy-MM-dd HH:mm:ss
  /// Example: 2023-09-05 23:36:03
  /// </summary>
  [JsonPropertyName("startTimeGMT")]
  public string StartTime { get; set; }

  [JsonPropertyName("beginTimestamp")]
  public long? BeginTimestamp { get; set; }
  
  [JsonPropertyName("distance")]
  public double? Distance { get; set; }

  [JsonPropertyName("duration")]
  public double? Duration { get; set; }

  /// <summary>
  /// Gets or sets the owner user profile identifier.
  /// </summary>
  /// <value>
  /// The owner user profile identifier.
  /// </value>
  [JsonPropertyName("ownerId")]
  public long OwnerId { get; set; }

  /// <summary>
  /// Gets or sets the type of the activity.
  /// </summary>
  /// <value>
  /// The type of the activity.
  /// </value>
  [JsonPropertyName("activityType")]
  public ActivityType ActivityType { get; set; }

  /// <summary>
  /// Gets or sets the type of the event.
  /// </summary>
  /// <value>
  /// The type of the event.
  /// </value>
  [JsonPropertyName("eventType")]
  public ActivityType EventType { get; set; }
}
