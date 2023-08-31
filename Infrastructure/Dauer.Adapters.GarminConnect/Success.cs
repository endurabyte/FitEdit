using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class Success
{
  [JsonPropertyName("internalId")]
  public int InternalId { get; set; }
}
