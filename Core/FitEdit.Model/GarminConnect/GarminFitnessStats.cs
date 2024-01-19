#nullable enable
using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

public class GarminFitnessStats
{
  [JsonPropertyName("date")]
  public DateTime Date { get; set; }

  [JsonPropertyName("countOfActivities")]
  public long CountOfActivities { get; set; }
}