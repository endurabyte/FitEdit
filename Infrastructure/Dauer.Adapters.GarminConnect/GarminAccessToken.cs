#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Adapters.GarminConnect;

public class GarminAccessToken
{
  [JsonPropertyName("access_token")]
  public required string AccessToken { get; set; }

  [JsonPropertyName("expires_in")]
  public int ExpiresIn { get; set; }

  [JsonPropertyName("jti")]
  public required string Jti { get; set; }

  [JsonPropertyName("refresh_token")]
  public required string RefreshToken { get; set; }

  [JsonPropertyName("refresh_token_expires_in")]
  public int RefreshTokenExpiresIn { get; set; }

  [JsonPropertyName("scope")]
  public required string Scope { get; set; }

  [JsonPropertyName("token_type")]
  public required string TokenType { get; set; }
}
