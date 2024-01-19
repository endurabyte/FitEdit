#nullable enable
using System.Text.Json.Serialization;

namespace FitEdit.Model.Strava;

public class StravaUploadStatus
{
  [JsonPropertyName("id")]
  public long Id { get; set; }

  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("progress")]
  public int Progress { get; set; }

  /// <summary>
  /// Values: new, analyzing, uploaded, error
  /// </summary>
  [JsonPropertyName("workflow")]
  public string? Workflow { get; set; }

  [JsonPropertyName("start_date")]
  public DateTime? StartDate { get; set; }

  [JsonPropertyName("error")]
  public string? Error { get; set; }

  [JsonPropertyName("activity")]
  public StravaActivity? Activity { get; set; }
}
