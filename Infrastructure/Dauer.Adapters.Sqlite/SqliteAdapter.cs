using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using SQLite;
using SQLitePCL;

namespace Dauer.Adapters.Sqlite;

public class SqliteAdapter : IDatabaseAdapter
{
  private readonly string dbPath_;
  private readonly SQLiteOpenFlags flags_ = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex;

  private SQLiteAsyncConnection db_;

  public SqliteAdapter(string dbPath)
  {
    dbPath_ = dbPath;
    _ = Task.Run(async () => db_ = await GetConnection().AnyContext());
  }

  private async Task<SQLiteAsyncConnection> GetConnection()
  {
    Log.Info($"{nameof(SqliteAdapter)}: Attempting to open database \'{dbPath_}\'");
    try
    {
      var connString = new SQLiteConnectionString(dbPath_, flags_, storeDateTimeAsTicks: false);
      var db = new SQLiteAsyncConnection(connString);
      await db.EnableWriteAheadLoggingAsync().AnyContext(); // TODO Call only once at DB creation
      await db.CreateTablesAsync(CreateFlags.None, new[] { typeof(SqliteFile) }).AnyContext();

      string providerName = raw.GetNativeLibraryName();
      Log.Info($"sqlite provider is {providerName}");
      return db;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return null;
    }
  }

  public virtual async Task<bool> InsertAsync(Model.BlobFile t) => 1 == await db_?.InsertAsync(t.Map()).AnyContext();
  public async Task UpdateAsync(Model.BlobFile t) => await db_?.UpdateAsync(t.Map()).AnyContext();
  public async Task DeleteAsync(Model.BlobFile t) => await db_?.DeleteAsync(t.Map()).AnyContext();
  public async Task<List<Model.BlobFile>> GetAllAsync()
  {
    Log.Info($"{nameof(SqliteAdapter)}.{nameof(GetAllAsync)}()");
    List<SqliteFile> files = await db_?.Table<SqliteFile>().ToListAsync().AnyContext();
    return files?.Select(SqliteFileMapper.Map).ToList();
  }
}