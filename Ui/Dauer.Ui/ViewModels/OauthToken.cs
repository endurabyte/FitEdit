using System.Text.Json.Serialization;

namespace Dauer.Ui.ViewModels;

public class OauthToken
{
  [JsonPropertyName("token")]
  public string? Token { get; set; }

}