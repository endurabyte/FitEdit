#nullable enable
using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Sensor
/// </summary>
public class Sensor
{
  /// <summary>
  /// Gets or sets the sku.
  /// </summary>
  /// <value>
  /// The sku.
  /// </value>
  [JsonPropertyName("sku")]
  public string? Sku { get; set; }

  /// <summary>
  /// Gets or sets the type of the source.
  /// </summary>
  /// <value>
  /// The type of the source.
  /// </value>
  [JsonPropertyName("sourceType")]
  public string? SourceType { get; set; }

  /// <summary>
  /// Gets or sets the software version.
  /// </summary>
  /// <value>
  /// The software version.
  /// </value>
  [JsonPropertyName("softwareVersion")]
  public double SoftwareVersion { get; set; }

  /// <summary>
  /// Gets or sets the type of the local device.
  /// </summary>
  /// <value>
  /// The type of the local device.
  /// </value>
  [JsonPropertyName("localDeviceType")]
  public string? LocalDeviceType { get; set; }
}