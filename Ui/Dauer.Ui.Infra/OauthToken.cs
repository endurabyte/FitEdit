using System.Text.Json.Serialization;

namespace Dauer.Ui.Infra;

public class OauthToken
{
  [JsonPropertyName("token")]
  public string? Token { get; set; }
}