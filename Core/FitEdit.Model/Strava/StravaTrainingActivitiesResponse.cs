#nullable enable
using System.Text.Json.Serialization;

namespace FitEdit.Model.Strava;

public class StravaTrainingActivitiesResponse
{
  [JsonPropertyName("models")]
  public List<StravaActivity> Models { get; set; } = new();

  [JsonPropertyName("page")]
  public int Page { get; set; }

  [JsonPropertyName("perPage")]
  public int PerPage { get; set; }

  [JsonPropertyName("total")]
  public int Total { get; set; }
}
