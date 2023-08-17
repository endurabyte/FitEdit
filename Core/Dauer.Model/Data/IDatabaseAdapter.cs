using System.ComponentModel;

namespace Dauer.Model.Data;

public interface IDatabaseAdapter : INotifyPropertyChanged
{
  bool Ready { get; }

  Task<bool> InsertAsync(Authorization t);
  Task DeleteAsync(Authorization t);
  Task<Authorization> GetAuthorizationAsync(string id);

  Task<bool> InsertAsync(MapTile t);
  Task DeleteAsync(MapTile t);
  Task<MapTile> GetMapTileAsync(string id);

  Task<bool> InsertAsync(DauerActivity t);
  Task DeleteAsync(DauerActivity t);
  Task<DauerActivity> GetActivityAsync(string id);

  Task DeleteAsync(BlobFile t);
  Task<List<BlobFile>> GetAllAsync();
  Task<bool> InsertAsync(BlobFile t);
  Task UpdateAsync(BlobFile t);
}