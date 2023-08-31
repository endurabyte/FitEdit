using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class Message
{
  [JsonPropertyName("code")]
  public string Code { get; set; }
}
