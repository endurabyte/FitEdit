using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class UploadUuid
{
  [JsonPropertyName("uuid")]
  public string Uuid { get; set; }
}
