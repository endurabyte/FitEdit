#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class Message
{
  [JsonPropertyName("code")]
  public int Code { get; set; }
  
  [JsonPropertyName("content")]
  public string? Content { get; set; }
}
