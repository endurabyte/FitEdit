using System.Text.Json.Serialization;

namespace Dauer.Ui.Desktop;

public class IsAuthorizedResponse
{
  [JsonPropertyName("message")]
  public string? Message { get; set; }
}
