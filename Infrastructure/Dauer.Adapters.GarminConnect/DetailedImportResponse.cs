using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class DetailedImportResponse
{
  [JsonPropertyName("detailedImportResult")]
  public DetailedImportResult DetailedImportResult { get; set; }
}
