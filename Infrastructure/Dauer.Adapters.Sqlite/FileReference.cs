#nullable enable
using SQLite;

namespace Dauer.Adapters.Sqlite;

public class FileReference 
{
  [PrimaryKey, NotNull]
  public string Id { get; set; } = string.Empty;

  [NotNull]
  public string Name { get; set; } = string.Empty;
}
