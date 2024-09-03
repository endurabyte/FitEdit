using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Activity image
/// </summary>
public class ActivityImage
{
  /// <summary>
  /// Gets or sets the image identifier.
  /// </summary>
  /// <value>
  /// The image identifier.
  /// </value>
  [JsonPropertyName("imageId")]
  public string? ImageId { get; set; }

  /// <summary>
  /// Gets or sets the URL.
  /// </summary>
  /// <value>
  /// The URL.
  /// </value>
  [JsonPropertyName("url")]
  public Uri? Url { get; set; }

  /// <summary>
  /// Gets or sets the small URL.
  /// </summary>
  /// <value>
  /// The small URL.
  /// </value>
  [JsonPropertyName("smallUrl")]
  public Uri? SmallUrl { get; set; }

  /// <summary>
  /// Gets or sets the medium URL.
  /// </summary>
  /// <value>
  /// The medium URL.
  /// </value>
  [JsonPropertyName("mediumUrl")]
  public Uri? MediumUrl { get; set; }

  /// <summary>
  /// Gets or sets the latitude.
  /// </summary>
  /// <value>
  /// The latitude.
  /// </value>
  [JsonPropertyName("latitude")]
  public object? Latitude { get; set; }

  /// <summary>
  /// Gets or sets the longitude.
  /// </summary>
  /// <value>
  /// The longitude.
  /// </value>
  [JsonPropertyName("longitude")]
  public object? Longitude { get; set; }

  /// <summary>
  /// Gets or sets the photo date.
  /// </summary>
  /// <value>
  /// The photo date.
  /// </value>
  [JsonPropertyName("photoDate")]
  public DateTimeOffset PhotoDate { get; set; }
}