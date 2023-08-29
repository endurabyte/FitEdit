#nullable enable

using SQLite;

namespace Dauer.Adapters.Sqlite;

public class AppSettings
{
  public const string DefaultKey = "FitEdit";

  [PrimaryKey]
  public string Id { get; set; } = DefaultKey;

  public DateTime? LastSynced { get; set; }
  public string? GarminUsername { get; set; }
  public string? GarminPassword { get; set; }
  public string? GarminCookies { get; set; }
  public string? StravaUsername { get; set; }
  public string? StravaPassword { get; set; }
}
