using SQLite;

namespace Dauer.Adapters.Sqlite;

public class SqliteFile 
{
  [PrimaryKey, AutoIncrement]
  public long Id { get; set; }

  public string Name { get; set; }
  public byte[] Bytes { get; set; }

  public SqliteFile() { }

  public SqliteFile(SqliteFile other)
  {
    Id = other.Id;
    Name = other.Name;
    Bytes = other.Bytes;
  }
}
