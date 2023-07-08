namespace Dauer.Adapters.Sqlite;

public static class SqliteFileMapper
{
  public static Model.Authorization Map(this Authorization a) => a == null ? null : new()
  {
    Id = a.Id,
    AccessToken = a.AccessToken,
    RefreshToken = a.RefreshToken,
    Expiry = a.Expiry,
  };

  public static Authorization Map(this Model.Authorization a) => new()
  {
    Id = a.Id,
    AccessToken = a.AccessToken,
    RefreshToken = a.RefreshToken,
    Expiry = a.Expiry,
  };

  public static Model.MapTile Map(this MapTile mt) => mt == null ? null : new()
  {
    Id = mt.Id,
    Bytes = mt.Bytes,
  };

  public static MapTile Map(this Model.MapTile mt) => new()
  {
    Id = mt.Id,
    Bytes = mt.Bytes,
  };

  public static Model.BlobFile Map(this SqliteFile f) => new()
  {
    Id = f.Id,
    Name = f.Name,
    Bytes = f.Bytes,
  };

  public static SqliteFile Map(this Model.BlobFile f) => new()
  {
    Id = f.Id,
    Name = f.Name,
    Bytes = f.Bytes,
  };
}