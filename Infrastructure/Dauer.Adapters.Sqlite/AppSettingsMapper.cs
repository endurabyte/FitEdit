#nullable enable

using System.Text.Json;

namespace Dauer.Adapters.Sqlite;

public static class AppSettingsMapper
{
  public static Model.AppSettings? MapModel(this AppSettings? entity) => entity == null ? null : new()
  {
    LastSynced = entity.LastSynced,
    GarminUsername = entity.GarminUsername,
    GarminPassword = entity.GarminPassword,
    GarminCookies = entity.GarminCookies == null 
      ? null
      : JsonSerializer.Deserialize<Dictionary<string, Model.Cookie>>(entity.GarminCookies),
    StravaUsername = entity.StravaUsername,
    StravaPassword = entity.StravaPassword,
    StravaCookies = entity.StravaCookies == null 
      ? null
      : JsonSerializer.Deserialize<Dictionary<string, Model.Cookie>>(entity.StravaCookies),
  };

  public static AppSettings MapEntity(this Model.AppSettings model) => new()
  {
    Id = AppSettings.DefaultKey,
    LastSynced = model.LastSynced,
    GarminUsername = model.GarminUsername,
    GarminPassword = model.GarminPassword,
    GarminCookies = JsonSerializer.Serialize(model.GarminCookies),
    StravaUsername = model.StravaUsername,
    StravaPassword = model.StravaPassword,
    StravaCookies = JsonSerializer.Serialize(model.StravaCookies),
  };
}
