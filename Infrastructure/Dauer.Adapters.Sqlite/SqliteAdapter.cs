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
      await db.CreateTablesAsync(CreateFlags.None, new[]
      {
        typeof(SqliteFile),
        typeof(MapTile),
        typeof(Authorization),
        typeof(DauerActivity)
      }).AnyContext();

      db_ = db;
      Log.Info($"{nameof(SqliteAdapter)} ready. sqlite provider is {raw.GetNativeLibraryName()}");
      Ready = true;
    }
    catch (Exception e)
    {
      Log.Error(e);
    }
  }

  public async Task<bool> InsertAsync(Model.Authorization t) => 1 == await db_?.InsertOrReplaceAsync(t.MapEntity()).AnyContext();
  public async Task DeleteAsync(Model.Authorization t) => await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.Authorization> GetAuthorizationAsync(string id) => (await GetAsync<Authorization>(id).AnyContext())?.MapModel();

  public async Task<bool> InsertAsync(Model.MapTile t) => 1 == await db_?.InsertOrReplaceAsync(t.MapEntity()).AnyContext();
  public async Task DeleteAsync(Model.MapTile t) => await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.MapTile> GetMapTileAsync(string id) => (await GetAsync<MapTile>(id).AnyContext())?.MapModel();

  public async Task<bool> InsertAsync(Model.DauerActivity t)
  {
    if (t.File != null)
    {
      bool ok = await InsertAsync(t.File).AnyContext();
      if (!ok) { return false; }

      t.File.Id = GetLastId();
    }

    return 1 == await db_?.InsertOrReplaceAsync(t.MapEntity()).AnyContext();
  }

  /// <summary>
  /// TODO this is not thread-safe
  /// </summary>
  private long GetLastId()
  {
    IDisposable myLock = null;
    try
    {
      SQLiteConnectionWithLock conn = db_.GetConnection();
      myLock = conn.Lock();
      long id = SQLite3.LastInsertRowid(conn.Handle);
      return id;
    }
    finally
    {
      myLock?.Dispose();
    }
  }

  public async Task DeleteAsync(Model.DauerActivity t)
  {
    if (t.File != null)
    {
      await db_?.DeleteAsync(t.File).AnyContext();
    }

    await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  }

  public async Task<Model.DauerActivity> GetActivityAsync(string id)
  {
    var e = await GetAsync<DauerActivity>(id).AnyContext();
    if (e == null) { return null; }

    Model.DauerActivity model = e.MapModel();
    model.File = e.FileId >= 0 
      ? await GetBlobFileAsync(e.FileId).AnyContext()
      : null;

    return model;
  }

  public virtual async Task<bool> InsertAsync(Model.BlobFile t) => 1 == await db_?.InsertAsync(t.MapEntity()).AnyContext();
  public async Task UpdateAsync(Model.BlobFile t) => await db_?.UpdateAsync(t.MapEntity()).AnyContext();
  public async Task DeleteAsync(Model.BlobFile t) => await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.BlobFile> GetBlobFileAsync(long id) => (await GetAsync<SqliteFile>(id).AnyContext()).MapModel();
  public async Task<List<Model.BlobFile>> GetAllAsync()
  {
    Log.Info($"{nameof(SqliteAdapter)}.{nameof(GetAllAsync)}()");
    List<SqliteFile> files = await db_?.Table<SqliteFile>().ToListAsync().AnyContext();
    return files?.Select(SqliteFileMapper.MapModel).ToList();
  }

  public async Task<T> GetAsync<T>(object key) where T : class, new()
  {
    try
    {
      return await db_?.GetAsync<T>(key).AnyContext();
    }
    catch (InvalidOperationException) // "Sequence contains no elements" => Not in db
    {
      return null;
    }
  }
}