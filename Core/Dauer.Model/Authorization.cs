namespace Dauer.Model;
#nullable enable

public class Authorization
{
  public string? Id { get; set; }
  public string? AccessToken { get; set; }
  public string? RefreshToken { get; set; }
  public DateTimeOffset Expiry { get; set; }
}
