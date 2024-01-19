using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Access control rule
/// </summary>
public class AccessControlRule
{
  /// <summary>
  /// Gets or sets the type identifier.
  /// </summary>
  /// <value>
  /// The type identifier.
  /// </value>
  [JsonPropertyName("typeId")]
  public long TypeId { get; set; }

  /// <summary>
  /// Gets or sets the type key.
  /// </summary>
  /// <value>
  /// The type key.
  /// </value>
  [JsonPropertyName("typeKey")]
  public string TypeKey { get; set; }
}