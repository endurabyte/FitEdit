using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Time zone
/// </summary>
public class TimeZoneUnit
{
  /// <summary>
  /// Gets or sets the unit identifier.
  /// </summary>
  /// <value>
  /// The unit identifier.
  /// </value>
  [JsonPropertyName("unitId")]
  public long UnitId { get; set; }

  /// <summary>
  /// Gets or sets the unit key.
  /// </summary>
  /// <value>
  /// The unit key.
  /// </value>
  [JsonPropertyName("unitKey")]
  public string UnitKey { get; set; }

  /// <summary>
  /// Gets or sets the factor.
  /// </summary>
  /// <value>
  /// The factor.
  /// </value>
  [JsonPropertyName("factor")]
  public long Factor { get; set; }

  /// <summary>
  /// Gets or sets the time zone.
  /// </summary>
  /// <value>
  /// The time zone.
  /// </value>
  [JsonPropertyName("timeZone")]
  public string TimeZone { get; set; }
}