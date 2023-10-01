#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Model.GarminConnect;

public class GarminLoginError
{
  [JsonPropertyName("error")]
  public string? Error { get; set; }
  [JsonPropertyName("errorText")]
  public string? ErrorText { get; set; }
}
