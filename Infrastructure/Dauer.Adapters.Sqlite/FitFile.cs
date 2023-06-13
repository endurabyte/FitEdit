using SQLite;

namespace Dauer.Adapters.Sqlite;

public class FitFile
{
  [PrimaryKey, AutoIncrement]
  public int Key { get; set; }
  public byte[] Data { get; set; }
}
