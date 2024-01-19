#nullable enable
using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Activity type
/// </summary>
public class ActivityType
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
  public string? TypeKey { get; set; }

  /// <summary>
  /// Gets or sets the parent type identifier.
  /// </summary>
  /// <value>
  /// The parent type identifier.
  /// </value>
  [JsonPropertyName("parentTypeId")]
  public long? ParentTypeId { get; set; }

  /// <summary>
  /// Gets or sets the sort order.
  /// </summary>
  /// <value>
  /// The sort order.
  /// </value>
  [JsonPropertyName("sortOrder")]
  public long SortOrder { get; set; }
}