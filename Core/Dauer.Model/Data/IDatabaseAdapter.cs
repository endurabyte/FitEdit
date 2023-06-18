namespace Dauer.Model.Data;

public interface IDatabaseAdapter
{
  Task DeleteAsync(BlobFile t);
  Task<List<BlobFile>> GetAllAsync();
  Task<bool> InsertAsync(BlobFile t);
  Task UpdateAsync(BlobFile t);
}

public class NullDatabaseAdapter : IDatabaseAdapter
{
  public Task DeleteAsync(BlobFile t) => Task.CompletedTask;
  public Task<List<BlobFile>> GetAllAsync() => Task.FromResult(new List<BlobFile>());
  public Task<bool> InsertAsync(BlobFile t) => Task.FromResult(true);
  public Task UpdateAsync(BlobFile t) => Task.CompletedTask;
}
