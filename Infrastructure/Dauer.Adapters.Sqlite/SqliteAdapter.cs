using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using SQLite;
using SQLitePCL;

namespace Dauer.Adapters.Sqlite;

public class SqliteAdapter : HasProperties, IDatabaseAdapter 
{
  private readonly string dbPath_;
  private readonly SQLiteOpenFlags flags_ = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex;

  private SQLiteAsyncConnection db_;

  private bool ready_;
  public bool Ready { get => ready_; set => Set(ref ready_, value); }

  public SqliteAdapter(string dbPath)
  {
    dbPath_ = dbPath;
    _ = Task.Run(async () => await OpenDatabase().AnyContext());
  }

  private async Task OpenDatabase()
  {
    Log.Info($"{nameof(SqliteAdapter)}: Attempting to open database \'{dbPath_}\'");
    try
    {
      var connString = new SQLiteConnectionString(dbPath_, flags_, storeDateTimeAsTicks: false);
      var db = new SQLiteAsyncConnection(connString);
      await db.EnableWriteAheadLoggingAsync().AnyContext(); // TODO Call only once at DB creation
      await db.CreateTablesAsync(CreateFlags.None, new[] { typeof(SqliteFile), typeof(MapTile), typeof(Authorization) }).AnyContext();

      db_ = db;
      Log.Info($"{nameof(SqliteAdapter)} ready. sqlite provider is {raw.GetNativeLibraryName()}");
      Ready = true;
    }
    catch (Exception e)
    {
      Log.Error(e);
    }
  }

  public async Task<bool> InsertAsync(Model.Authorization t) => 1 == await db_?.InsertOrReplaceAsync(t.Map()).AnyContext();
  public async Task DeleteAsync(Model.Authorization t) => await db_?.DeleteAsync(t.Map()).AnyContext();
  public async Task<Model.Authorization> GetAuthorizationAsync(string id)
  {
    try
    {
      var auth = await db_?.GetAsync<Authorization>(id).AnyContext();
      return auth.Map();
    }
    catch (InvalidOperationException) // "Sequence contains no elements" => Not in db
    {
      return null;
    }
  }

  public async Task<bool> InsertAsync(Model.MapTile t) => 1 == await db_?.InsertOrReplaceAsync(t.Map()).AnyContext();
  public async Task DeleteAsync(Model.MapTile t) => await db_?.DeleteAsync(t.Map()).AnyContext();
  public async Task<Model.MapTile> GetMapTileAsync(string id)
  {
    try
    {
      var tile = await db_?.GetAsync<MapTile>(id).AnyContext();
      return tile.Map();
    }
    catch (InvalidOperationException) // "Sequence contains no elements" => Not in db
    {
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