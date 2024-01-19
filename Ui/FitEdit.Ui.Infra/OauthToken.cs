using System.Text.Json.Serialization;

namespace FitEdit.Ui.Infra;

public class OauthToken
{
  [JsonPropertyName("token")]
  public string? Token { get; set; }
}