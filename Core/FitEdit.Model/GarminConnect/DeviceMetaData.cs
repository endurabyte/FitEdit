using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Device metadata
/// </summary>
public class DeviceMetaData
{
  /// <summary>
  /// Gets or sets the device identifier.
  /// </summary>
  /// <value>
  /// The device identifier.
  /// </value>
  [JsonPropertyName("deviceId")]
  public string DeviceId { get; set; }

  /// <summary>
  /// Gets or sets the device type pk.
  /// </summary>
  /// <value>
  /// The device type pk.
  /// </value>
  [JsonPropertyName("deviceTypePk")]
  public long DeviceTypePk { get; set; }

  /// <summary>
  /// Gets or sets the device version pk.
  /// </summary>
  /// <value>
  /// The device version pk.
  /// </value>
  [JsonPropertyName("deviceVersionPk")]
  public long DeviceVersionPk { get; set; }
}