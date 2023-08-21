#nullable enable

using SQLite;

namespace Dauer.Adapters.Sqlite;

public class AppSettings
{
  public const string DefaultKey = "FitEdit";

  [PrimaryKey]
  public string Id { get; set; } = DefaultKey;

  public DateTime? LastSynced { get; set; }
}
