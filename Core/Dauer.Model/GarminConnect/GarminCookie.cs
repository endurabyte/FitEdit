#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

public class GarminCookie
{
  [JsonPropertyName("name")]
  public string? Name { get; set; }
  [JsonPropertyName("value")]
  public string? Value { get; set; }
  [JsonPropertyName("domain")]
  public string? Domain { get; set; }
  [JsonPropertyName("path")]
  public string? Path { get; set; }
}
