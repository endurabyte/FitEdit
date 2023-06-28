namespace Dauer.Model.Data;

public interface IDatabaseAdapter
{
  Task<bool> InsertAsync(MapTile t);
  Task DeleteAsync(MapTile t);
  Task<MapTile> GetAsync(string id);

  Task DeleteAsync(BlobFile t);
  Task<List<BlobFile>> GetAllAsync();
  Task<bool> InsertAsync(BlobFile t);
  Task UpdateAsync(BlobFile t);
}