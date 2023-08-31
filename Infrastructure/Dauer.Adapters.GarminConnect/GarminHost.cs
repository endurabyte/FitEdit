using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class GarminHost
{
  [JsonPropertyName("host")]
  public string Host { get; set; }
}
