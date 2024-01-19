using SQLite;

namespace FitEdit.Adapters.Sqlite;

public class MapTile 
{
  [PrimaryKey]
  public string Id { get; set; }
  public byte[] Bytes { get; set; }
}
