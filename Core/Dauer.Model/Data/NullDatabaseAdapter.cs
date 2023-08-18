namespace Dauer.Model.Data;

public class NullDatabaseAdapter : HasProperties, IDatabaseAdapter
{
  public bool Ready => true;

  public Task<bool> InsertAsync(Authorization t) => Task.FromResult(true);
  public Task DeleteAsync(Authorization t) => Task.CompletedTask;
  public Task<Authorization> GetAuthorizationAsync(string id) => Task.FromResult(new Authorization { Id = id });

  public Task<bool> InsertAsync(MapTile t) => Task.FromResult(true);
  public Task DeleteAsync(MapTile t) => Task.CompletedTask;
  public Task<MapTile> GetMapTileAsync(string id) => Task.FromResult(new MapTile { Id = id });

  public Task<bool> InsertAsync(DauerActivity t) => Task.FromResult(true);
  public Task<bool> UpdateAsync(DauerActivity t) => Task.FromResult(true);
  public Task<bool> DeleteAsync(DauerActivity t) => Task.FromResult(true);
  public Task<DauerActivity> GetActivityAsync(string id) => Task.FromResult(new DauerActivity { Id = id });
  public Task<List<DauerActivity>> GetAllActivitiesAsync() => Task.FromResult(new List<DauerActivity>());
  public Task<List<string>> GetAllActivityIdsAsync() => Task.FromResult(new List<string>());

  public Task<bool> DeleteAsync(FileReference t) => Task.FromResult(true);
  public Task<bool> InsertAsync(FileReference t) => Task.FromResult(true);
  public Task UpdateAsync(FileReference t) => Task.CompletedTask;
}
