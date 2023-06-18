namespace Dauer.Adapters.Sqlite;

public static class SqliteFileMapper
{
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