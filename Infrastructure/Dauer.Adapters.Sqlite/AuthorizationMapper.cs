namespace Dauer.Adapters.Sqlite;

public static class AuthorizationMapper
{
  public static Model.Authorization MapModel(this Authorization a) => a == null ? null : new()
  {
    Id = a.Id,
    AccessToken = a.AccessToken,
    RefreshToken = a.RefreshToken,
    IdentityToken = a.IdentityToken,
    Sub = a.Sub,
    Created = a.Created,
    Expiry = a.Expiry,
    Username = a.Username,
  };

  public static Authorization MapEntity(this Model.Authorization a) => new()
  {
    Id = a.Id,
    AccessToken = a.AccessToken,
    RefreshToken = a.RefreshToken,
    IdentityToken = a.IdentityToken,
    Sub = a.Sub,
    Created = a.Created,
    Expiry = a.Expiry,
    Username = a.Username,
  };
}
