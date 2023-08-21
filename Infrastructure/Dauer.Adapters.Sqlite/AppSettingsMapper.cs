#nullable enable

namespace Dauer.Adapters.Sqlite;

public static class AppSettingsMapper
{
  public static Model.AppSettings? MapModel(this AppSettings? entity) => entity == null ? null : new()
  {
    LastSynced = entity.LastSynced,
  };

  public static AppSettings MapEntity(this Model.AppSettings model) => new()
  {
    Id = AppSettings.DefaultKey,
    LastSynced = model.LastSynced,
  };
}
