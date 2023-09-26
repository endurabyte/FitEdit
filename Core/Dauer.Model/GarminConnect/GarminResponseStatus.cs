#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

public class GarminResponseStatus
{
  [JsonPropertyName("httpStatus")]
  public string? HttpStatus { get; set; }
  [JsonPropertyName("message")]
  public string? Message { get; set; }
  [JsonPropertyName("type")]
  public string? Type { get; set; }
}