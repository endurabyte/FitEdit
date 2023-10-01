#nullable enable
namespace Dauer.Model.GarminConnect;

/// <summary>
/// Configuration for login to Garmin Connect
/// </summary>
public class GarminConnectConfig
{
  public string? Username { get; set; }
  public string? Password { get; set; }
  public string? SsoId { get; set; }
  public string? SessionId { get; set; }
  public string? JwtId { get; set; }
  public GarminAccessToken? Token { get; set; }
}