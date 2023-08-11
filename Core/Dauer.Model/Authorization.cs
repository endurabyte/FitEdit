namespace Dauer.Model;
#nullable enable

public class Authorization
{
  public string? Id { get; set; }
  public string? AccessToken { get; set; }
  public string? RefreshToken { get; set; }
  public string? IdentityToken { get; set; }
  public DateTimeOffset Created { get; set; }
  public DateTimeOffset Expiry { get; set; }

  public string? Username { get; set; }

  public Authorization()
  {

  }

  public Authorization(Authorization? other)
  {
    if (other is null) { return; }

    Id = other.Id;
    AccessToken = other.AccessToken;
    RefreshToken = other.RefreshToken;
    IdentityToken = other.IdentityToken;
    Created = other.Created;
    Expiry = other.Expiry;
    Username = other.Username;
  }
}
