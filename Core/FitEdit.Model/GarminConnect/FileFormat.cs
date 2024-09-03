using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// File format
/// </summary>
public class FileFormat
{
  /// <summary>
  /// Gets or sets the format identifier.
  /// </summary>
  /// <value>
  /// The format identifier.
  /// </value>
  [JsonPropertyName("formatId")]
  public long FormatId { get; set; }

  /// <summary>
  /// Gets or sets the format key.
  /// </summary>
  /// <value>
  /// The format key.
  /// </value>
  [JsonPropertyName("formatKey")]
  public string? FormatKey { get; set; }
}