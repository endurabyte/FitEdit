using System.Text.Json.Serialization;

namespace FitEdit.Adapters.GarminConnect;

public class Success
{
  [JsonPropertyName("internalId")]
  public int InternalId { get; set; }
}
