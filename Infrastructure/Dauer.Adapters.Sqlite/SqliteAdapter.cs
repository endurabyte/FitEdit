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
        typeof(FileReference),
        typeof(MapTile),
        typeof(Authorization),
        typeof(DauerActivity),
        typeof(AppSettings),
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
  public async Task<bool> UpdateAsync(Model.Authorization t) => 1 == await db_?.UpdateAsync(t.MapEntity()).AnyContext();
  public async Task DeleteAsync(Model.Authorization t) => await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.Authorization> GetAuthorizationAsync(string id) => (await GetAsync<Authorization>(id).AnyContext())?.MapModel();

  public async Task<bool> InsertAsync(Model.MapTile t) => 1 == await db_?.InsertOrReplaceAsync(t.MapEntity()).AnyContext();
  public async Task DeleteAsync(Model.MapTile t) => await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.MapTile> GetMapTileAsync(string id) => (await GetAsync<MapTile>(id).AnyContext())?.MapModel();

  public async Task<bool> InsertAsync(Model.DauerActivity a)
  {
    if (a.File != null)
    {
      bool ok = await InsertAsync(a.File).AnyContext();
      if (!ok) { return false; }
    }

    return 1 == await db_?.InsertOrReplaceAsync(a.MapEntity()).AnyContext();
  }

  public async Task<bool> UpdateAsync(Model.DauerActivity a)
  {
    if (a.File != null) 
    {
      bool ok = 1 == await db_?.InsertOrReplaceAsync(a.File.MapEntity()).AnyContext();
      if (!ok) { return false; }  
    }

    return 1 == await db_.UpdateAsync(a.MapEntity()).AnyContext();
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

  public async Task<bool> DeleteAsync(Model.DauerActivity t)
  {
    if (t.File != null)
    {
      bool ok = await DeleteAsync(t.File).AnyContext();
      if (!ok) { return false; }
    }

    return 1 == await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  }

  public async Task<Model.DauerActivity> GetActivityAsync(string id)
  {
    var a = await GetAsync<DauerActivity>(id).AnyContext();
    if (a == null) { return null; }

    Model.DauerActivity model = a.MapModel();
    model.File = a.FileId != null
      ? await GetFileReferenceAsync(a.FileId).AnyContext()
      : null;

    return model;
  }

  public async Task<List<Model.DauerActivity>> GetAllActivitiesAsync()
  {
    Log.Info($"{nameof(SqliteAdapter)}.{nameof(GetAllActivitiesAsync)}()");
    List<DauerActivity> activities = await db_?.Table<DauerActivity>().ToListAsync().AnyContext();

    var models = new List<Model.DauerActivity>(activities.Count);

    foreach (var a in activities)
    {
      Model.DauerActivity model = a.MapModel();
      model.File = a.FileId != null
        ? await GetFileReferenceAsync(a.FileId).AnyContext()
        : null;
      models.Add(model);
    }
    return models;
  }

  public async Task<List<string>> GetAllActivityIdsAsync() => await db_
    .QueryScalarsAsync<string>($"SELECT Id from {nameof(DauerActivity)}")
    .AnyContext();

  public virtual async Task<bool> InsertAsync(Model.FileReference t) => 1 == await db_?.InsertAsync(t.MapEntity()).AnyContext();
  public async Task UpdateAsync(Model.FileReference t) => await db_?.UpdateAsync(t.MapEntity()).AnyContext();
  public async Task<bool> DeleteAsync(Model.FileReference t) => 1 == await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.FileReference> GetFileReferenceAsync(string id) => (await GetAsync<FileReference>(id).AnyContext()).MapModel();

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

  public async Task<bool> InsertOrUpdateAsync(Model.AppSettings a) => 1 == await db_?.InsertOrReplaceAsync(a.MapEntity()).AnyContext();

  public async Task<Model.AppSettings> GetAppSettingsAsync() => (await GetAsync<AppSettings>(AppSettings.DefaultKey)
    .AnyContext())
    .MapModel();
}