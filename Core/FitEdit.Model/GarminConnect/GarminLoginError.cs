#nullable enable
using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

public class GarminLoginError
{
  [JsonPropertyName("error")]
  public string? Error { get; set; }
  [JsonPropertyName("errorText")]
  public string? ErrorText { get; set; }
}
