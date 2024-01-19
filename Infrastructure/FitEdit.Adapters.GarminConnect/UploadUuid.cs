using System.Text.Json.Serialization;

namespace FitEdit.Adapters.GarminConnect;

public class UploadUuid
{
  [JsonPropertyName("uuid")]
  public string Uuid { get; set; }
}
