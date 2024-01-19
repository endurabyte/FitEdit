using System.Text.Json.Serialization;

namespace FitEdit.Ui.Desktop;

public class IsAuthorizedResponse
{
  [JsonPropertyName("message")]
  public string? Message { get; set; }
}
