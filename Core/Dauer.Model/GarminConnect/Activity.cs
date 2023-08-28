using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

/// <summary>
/// Activity
/// </summary>
public class Activity
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
  /// Gets or sets the user profile identifier.
  /// </summary>
  /// <value>
  /// The user profile identifier.
  /// </value>
  [JsonPropertyName("userProfileId")]
  public long UserProfileId { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether this instance is multi sport parent.
  /// </summary>
  /// <value>
  ///   <c>true</c> if this instance is multi sport parent; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("isMultiSportParent")]
  public bool IsMultiSportParent { get; set; }

  /// <summary>
  /// Gets or sets the type of the activity.
  /// </summary>
  /// <value>
  /// The type of the activity.
  /// </value>
  [JsonPropertyName("activityTypeDTO")]
  public ActivityType ActivityType { get; set; }

  /// <summary>
  /// Gets or sets the type of the activity.
  /// Overloaded JsonPropertyName for the same property - "activityTypeDTO".
  /// </summary>
  /// <value>
  /// The type of the activity.
  /// </value>
  [JsonPropertyName("activityType")]
#pragma warning disable IDE0051 // Remove unused private members
  private ActivityType ActivityTypeInternal_ { set => ActivityType = value; }
#pragma warning restore IDE0051 // Remove unused private members

  /// <summary>
  /// Gets or sets the type of the event.
  /// </summary>
  /// <value>
  /// The type of the event.
  /// </value>
  [JsonPropertyName("eventTypeDTO")]
  public ActivityType EventType { get; set; }

  /// <summary>
  /// Gets or sets the access control rule.
  /// </summary>
  /// <value>
  /// The access control rule.
  /// </value>
  [JsonPropertyName("accessControlRuleDTO")]
  public AccessControlRule AccessControlRule { get; set; }

  /// <summary>
  /// Gets or sets the time zone unit.
  /// </summary>
  /// <value>
  /// The time zone unit.
  /// </value>
  [JsonPropertyName("timeZoneUnitDTO")]
  public TimeZoneUnit TimeZoneUnit { get; set; }

  /// <summary>
  /// Gets or sets the metadata.
  /// </summary>
  /// <value>
  /// The metadata.
  /// </value>
  [JsonPropertyName("metadataDTO")]
  public Metadata Metadata { get; set; }

  /// <summary>
  /// Gets or sets the summary.
  /// </summary>
  /// <value>
  /// The summary.
  /// </value>
  [JsonPropertyName("summaryDTO")]
  public Summary Summary { get; set; }

  /// <summary>
  /// Gets or sets the name of the location.
  /// </summary>
  /// <value>
  /// The name of the location.
  /// </value>
  [JsonPropertyName("locationName")]
  public string LocationName { get; set; }
}
