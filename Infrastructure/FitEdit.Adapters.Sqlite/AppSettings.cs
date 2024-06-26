﻿#nullable enable

using SQLite;

namespace FitEdit.Adapters.Sqlite;

public class AppSettings
{
  public const string DefaultKey = "FitEdit";

  [PrimaryKey]
  public string Id { get; set; } = DefaultKey;

  public DateTime? LastSynced { get; set; }
  public string? GarminUsername { get; set; }
  public string? GarminPassword { get; set; }
  public string? GarminCookies { get; set; }
  public string? GarminSsoId { get; set; }
  public string? GarminSessionId { get; set; }
  public string? StravaUsername { get; set; }
  public string? StravaPassword { get; set; }
  public string? StravaCookies { get; set; }
}
