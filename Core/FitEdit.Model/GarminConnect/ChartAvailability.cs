using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Chart availability
/// </summary>
public class ChartAvailability
{
  /// <summary>
  /// Gets or sets a value indicating whether [show air temperature].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show air temperature]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showAirTemperature")]
  public bool ShowAirTemperature { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show distance].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show distance]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showDistance")]
  public bool ShowDistance { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show duration].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show duration]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showDuration")]
  public bool ShowDuration { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show elevation].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show elevation]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showElevation")]
  public bool ShowElevation { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show heart rate].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show heart rate]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showHeartRate")]
  public bool ShowHeartRate { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show moving duration].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show moving duration]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showMovingDuration")]
  public bool ShowMovingDuration { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show moving speed].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show moving speed]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showMovingSpeed")]
  public bool ShowMovingSpeed { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show run cadence].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show run cadence]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showRunCadence")]
  public bool ShowRunCadence { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show speed].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show speed]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showSpeed")]
  public bool ShowSpeed { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [show timestamp].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [show timestamp]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("showTimestamp")]
  public bool ShowTimestamp { get; set; }
}