using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class Failure
{
  [JsonPropertyName("messages")]
  public List<Message> Messages { get; set; }
}
