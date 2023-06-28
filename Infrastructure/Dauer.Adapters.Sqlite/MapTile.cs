using SQLite;

namespace Dauer.Adapters.Sqlite;

public class MapTile 
{
  [PrimaryKey]
  public string Id { get; set; }
  public byte[] Bytes { get; set; }
}
