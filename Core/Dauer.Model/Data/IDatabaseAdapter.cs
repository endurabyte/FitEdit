namespace Dauer.Model.Data;

public interface IDatabaseAdapter
{
  Task DeleteAsync<T>(T t);
  Task<List<T>> GetAllAsync<T>() where T : new();
  Task InsertAsync<T>(T t);
  Task UpdateAsync<T>(T t);
}

