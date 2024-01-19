#nullable enable
using System.Text.Json.Serialization;

namespace FitEdit.Adapters.GarminConnect;

public class Failure
{
  [JsonPropertyName("internalId")]
  public long? InternalId { get; set; }

  [JsonPropertyName("externalId")]
  public string? ExternalId { get; set; }

  [JsonPropertyName("messages")]
  public required List<Message> Messages { get; set; }
}
