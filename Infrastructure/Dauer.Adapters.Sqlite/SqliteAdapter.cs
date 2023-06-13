using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using SQLite;

namespace Dauer.Adapters.Sqlite;

public class SqliteAdapter : IDatabaseAdapter
{
  private readonly string dbPath_;

  private readonly SQLiteOpenFlags flags_ = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

  private readonly AsyncLazy<SQLiteAsyncConnection> db_;

  public SqliteAdapter(string dbPath)
  {
    dbPath_ = dbPath;
    db_ = new(async () =>
    {
      var db = new SQLiteAsyncConnection(dbPath_, flags_);
      await db.EnableWriteAheadLoggingAsync().AnyContext();
      await db.CreateTablesAsync(CreateFlags.None, new[] { typeof(FitFile) }).AnyContext();
      return db;
    });
  }

  public async Task InsertAsync<T>(T t) => await db_.Value.InsertAsync(t).AnyContext();
  public async Task UpdateAsync<T>(T t) => await db_.Value.UpdateAsync(t).AnyContext();
  public async Task DeleteAsync<T>(T t) => await db_.Value.DeleteAsync(t).AnyContext();
  public async Task<List<T>> GetAllAsync<T>() where T : new() => await db_.Value.Table<T>().ToListAsync().AnyContext();
}