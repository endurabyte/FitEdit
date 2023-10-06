using System.ComponentModel;

namespace Dauer.Model.Data;

public interface IDatabaseAdapter : INotifyPropertyChanged
{
  bool Ready { get; }

  Task<bool> InsertAsync(Authorization t);
  Task<bool> UpdateAsync(Authorization t);
  Task DeleteAsync(Authorization t);
  Task<Authorization> GetAuthorizationAsync(string id);

  Task<bool> InsertAsync(MapTile t);
  Task DeleteAsync(MapTile t);
  Task<MapTile> GetMapTileAsync(string id);

  Task<bool> InsertOrUpdateAsync(AppSettings a);
  Task<AppSettings> GetAppSettingsAsync();

  Task<bool> InsertAsync(LocalActivity t);
  Task<bool> UpdateAsync(LocalActivity t);
  Task<bool> DeleteAsync(LocalActivity t);
  Task<LocalActivity> GetActivityAsync(string id);
  Task<List<LocalActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before, int limit);
  Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before);
  Task<bool> ActivityExistsAsync(string id);

  Task<bool> DeleteAsync(FileReference t);
  Task<bool> InsertAsync(FileReference t);
  Task UpdateAsync(FileReference t);
}