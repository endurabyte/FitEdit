namespace Dauer.Model.Data;

public class NullDatabaseAdapter : HasProperties, IDatabaseAdapter
{
  public bool Ready => true;

  public Task<bool> InsertAsync(Authorization t) => Task.FromResult(true);
  public Task<bool> UpdateAsync(Authorization t) => Task.FromResult(true);
  public Task DeleteAsync(Authorization t) => Task.CompletedTask;
  public Task<Authorization> GetAuthorizationAsync(string id) => Task.FromResult(new Authorization { Id = id });

  public Task<bool> InsertAsync(MapTile t) => Task.FromResult(true);
  public Task DeleteAsync(MapTile t) => Task.CompletedTask;
  public Task<MapTile> GetMapTileAsync(string id) => Task.FromResult(new MapTile { Id = id });

  public Task<bool> InsertAsync(LocalActivity t) => Task.FromResult(true);
  public Task<bool> UpdateAsync(LocalActivity t) => Task.FromResult(true);
  public Task<bool> DeleteAsync(LocalActivity t) => Task.FromResult(true);
  public Task<LocalActivity> GetActivityAsync(string id) => Task.FromResult(new LocalActivity { Id = id });
  public Task<List<LocalActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before, int limit) => Task.FromResult(new List<LocalActivity>());
  public Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before) => Task.FromResult(new List<string>());
  public Task<LocalActivity> GetByIdOrStartTimeAsync(string id, DateTime startTime) => Task.FromResult(new LocalActivity());

  public Task<bool> DeleteAsync(FileReference t) => Task.FromResult(true);
  public Task<bool> InsertAsync(FileReference t) => Task.FromResult(true);
  public Task UpdateAsync(FileReference t) => Task.CompletedTask;

  public Task<bool> InsertOrUpdateAsync(AppSettings a) => Task.FromResult(true);
  public Task<AppSettings> GetAppSettingsAsync() => Task.FromResult(new AppSettings());
}
