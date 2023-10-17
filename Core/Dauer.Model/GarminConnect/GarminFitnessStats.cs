#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

public class GarminFitnessStats
{
  [JsonPropertyName("date")]
  public DateTime Date { get; set; }

  [JsonPropertyName("countOfActivities")]
  public long CountOfActivities { get; set; }
}