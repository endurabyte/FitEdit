#nullable enable

using Dauer.Model.Data;

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
      : Json.MapFromJson<Dictionary<string, Model.Cookie>>(entity.GarminCookies),
    GarminSsoId = entity.GarminSsoId,
    GarminSessionId = entity.GarminSessionId,
    StravaUsername = entity.StravaUsername,
    StravaPassword = entity.StravaPassword,
    StravaCookies = entity.StravaCookies == null 
      ? null
      : Json.MapFromJson<Dictionary<string, Model.Cookie>>(entity.StravaCookies),
  };

  public static AppSettings MapEntity(this Model.AppSettings model) => new()
  {
    Id = AppSettings.DefaultKey,
    LastSynced = model.LastSynced,
    GarminUsername = model.GarminUsername,
    GarminPassword = model.GarminPassword,
    GarminSsoId = model.GarminSsoId,
    GarminSessionId = model.GarminSessionId,
    GarminCookies = Json.ToJson(model.GarminCookies),
    StravaUsername = model.StravaUsername,
    StravaPassword = model.StravaPassword,
    StravaCookies = Json.ToJson(model.StravaCookies),
  };
}
