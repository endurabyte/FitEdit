using System.Text.Json.Serialization;

namespace FitEdit.Adapters.GarminConnect;

public class DetailedImportResponse
{
  [JsonPropertyName("detailedImportResult")]
  public DetailedImportResult DetailedImportResult { get; set; }
}
