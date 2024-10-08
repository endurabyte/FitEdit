﻿using FitEdit.Model;
using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using FitEdit.Services;
using SQLite;
using SQLitePCL;

namespace FitEdit.Adapters.Sqlite;

public class SqliteAdapter : HasProperties, IDatabaseAdapter
{
  private readonly string dbPath_;
  private readonly ICryptoService crypto_;
  private readonly SQLiteOpenFlags flags_ = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex;

  private SQLiteAsyncConnection db_;

  private bool ready_;
  public bool Ready { get => ready_; set => Set(ref ready_, value); }

  public SqliteAdapter(string dbPath, ICryptoService crypto)
  {
    dbPath_ = dbPath;
    crypto_ = crypto;
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
        typeof(LocalActivity),
        typeof(AppSettings),
      }).AnyContext();

      db_ = db;

      await MigrateDataAsync();

      Log.Info($"{nameof(SqliteAdapter)} ready. sqlite provider is {raw.GetNativeLibraryName()}");
      Ready = true;
    }
    catch (Exception e)
    {
      Log.Error(e);
    }
  }

  /// <summary>
  /// Migrate <see cref="LocalActivity.StartTime"/> to <see cref="LocalActivity.StartTimeUnix"/>
  /// </summary>
  private async Task MigrateDataAsync()
  {
    await CreateIndex_LocalActivity_StartTimeUnix().AnyContext();
    await CreateIndex_LocalActivity_SourceId().AnyContext();

    List<LocalActivity> activities = await db_?.Table<LocalActivity>()
      .Where(a => a.StartTimeUnix == null)
      .ToListAsync()
      .AnyContext();

    foreach (var activity in activities)
    {
      activity.StartTimeUnix = activity.StartTime.GetUnixTimestamp();
      await db_?.UpdateAsync(activity).AnyContext();
    }
  }

  private async Task CreateIndex_LocalActivity_StartTimeUnix()
  {
    string query = "CREATE INDEX IF NOT EXISTS Idx_LocalActivity_StartTimeUnix ON LocalActivity(StartTimeUnix);";
    await db_?.ExecuteAsync(query).AnyContext();
  }

  private async Task CreateIndex_LocalActivity_SourceId()
  {
    string query = "CREATE INDEX IF NOT EXISTS Idx_LocalActivity_SourceId ON LocalActivity(SourceId);";
    await db_?.ExecuteAsync(query).AnyContext();
  }

  public async Task<bool> InsertAsync(Model.Authorization t) => 1 == await db_?.InsertOrReplaceAsync(t.MapEntity()).AnyContext();
  public async Task<bool> UpdateAsync(Model.Authorization t) => 1 == await db_?.UpdateAsync(t.MapEntity()).AnyContext();
  public async Task DeleteAsync(Model.Authorization t) => await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.Authorization> GetAuthorizationAsync(string id) => (await GetAsync<Authorization>(id).AnyContext())?.MapModel();

  public async Task<bool> InsertAsync(Model.MapTile t) => 1 == await db_?.InsertOrReplaceAsync(t.MapEntity()).AnyContext();
  public async Task DeleteAsync(Model.MapTile t) => await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  public async Task<Model.MapTile> GetMapTileAsync(string id) => (await GetAsync<MapTile>(id).AnyContext())?.MapModel();

  public async Task<bool> InsertAsync(Model.LocalActivity a)
  {
    if (a.File != null)
    {
      bool ok = await InsertAsync(a.File).AnyContext();
      if (!ok) { return false; }
    }

    return 1 == await db_?.InsertOrReplaceAsync(a.MapEntity()).AnyContext();
  }

  public async Task<bool> UpdateAsync(Model.LocalActivity a)
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

  public async Task<bool> DeleteAsync(Model.LocalActivity t)
  {
    if (t.File != null)
    {
      bool ok = await DeleteAsync(t.File).AnyContext();
      if (!ok) { return false; }
    }

    return 1 == await db_?.DeleteAsync(t.MapEntity()).AnyContext();
  }

  public async Task<Model.LocalActivity> GetActivityAsync(string id)
  {
    var a = await GetAsync<LocalActivity>(id).AnyContext();
    if (a == null) { return null; }

    Model.LocalActivity model = a.MapModel();
    model.File = a.FileId != null
      ? await GetFileReferenceAsync(a.FileId).AnyContext()
      : null;

    return model;
  }

  public async Task<Model.LocalActivity> GetByIdOrStartTimeAsync(string id, DateTime startTime)
  {
    LocalActivity a = await GetByPropertyOrStartTimeAsync(id, nameof(LocalActivity.Id), startTime);

    if (a == null) { return null; }

    Model.LocalActivity model = a.MapModel();
    model.File = a.FileId != null
      ? await GetFileReferenceAsync(a.FileId).AnyContext()
      : null;

    return model;
  }

  public async Task<Model.LocalActivity> GetBySourceIdOrStartTimeAsync(string sourceId, DateTime startTime)
  {
    LocalActivity a = await GetByPropertyOrStartTimeAsync(sourceId, nameof(LocalActivity.SourceId), startTime);

    if (a == null) { return null; }

    Model.LocalActivity model = a.MapModel();
    model.File = a.FileId != null
      ? await GetFileReferenceAsync(a.FileId).AnyContext()
      : null;

    return model;
  }

  private async Task<LocalActivity> GetByPropertyOrStartTimeAsync<T>(T property, string propertyName, DateTime startTime, TimeSpan? margin = default)
  {
    margin ??= TimeSpan.FromSeconds(2);
    long marginS = (long)margin.Value.TotalSeconds;
    long startTimeUnix = startTime.GetUnixTimestamp();

    long lowerBound = startTimeUnix - marginS;
    long upperBound = startTimeUnix + marginS;

    string query = $@"
SELECT * FROM {nameof(LocalActivity)}
WHERE {propertyName} = ? OR (StartTime BETWEEN {lowerBound} AND {upperBound})
LIMIT 1";

    List<LocalActivity> res = await db_?.QueryAsync<LocalActivity>(query, property).AnyContext();
    LocalActivity la = res.FirstOrDefault();

    return la;
  }

  public async Task<List<Model.LocalActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before, int limit)
  {
    Log.Info($"{nameof(SqliteAdapter)}.{nameof(GetAllActivitiesAsync)}()");
    List<LocalActivity> activities = await db_?
      .Table<LocalActivity>()
      .Where(act => (after == null || act.StartTime >= after) && (before == null || act.StartTime < before))
      .OrderByDescending(act => act.StartTime)
      .Take(limit)
      .ToListAsync()
      .AnyContext();

    var models = new List<Model.LocalActivity>(activities.Count);

    foreach (var a in activities)
    {
      Model.LocalActivity model = a.MapModel();
      model.File = a.FileId != null
        ? await GetFileReferenceAsync(a.FileId).AnyContext()
        : null;
      models.Add(model);
    }
    return models;
  }

  public async Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before) => (await db_?
    .Table<LocalActivity>()
      .Where(act => (after == null || act.StartTime >= after) && (before == null || act.StartTime < before))
      .ToListAsync().AnyContext())?
    .Select(act => act.Id).ToList() ?? new List<string>();

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

  public async Task<bool> InsertOrUpdateAsync(Model.AppSettings a)
  { 
    AppSettings e = a.MapEntity();
    e.GarminPassword = crypto_.Encrypt(e.GarminUsername, e.GarminPassword);
    e.GarminCookies = crypto_.Encrypt(e.GarminSsoId, e.GarminCookies);
    e.StravaPassword = crypto_.Encrypt(e.StravaUsername, e.StravaPassword);
    e.StravaCookies = crypto_.Encrypt(e.StravaUsername, e.StravaCookies);

    return 1 == await db_?.InsertOrReplaceAsync(e).AnyContext();
  }

  public async Task<Model.AppSettings> GetAppSettingsAsync()
  {
    AppSettings e = await GetAsync<AppSettings>(AppSettings.DefaultKey).AnyContext();

    if (e is null) { return null; }

    e.GarminPassword = crypto_.Decrypt(e.GarminUsername, e.GarminPassword);
    e.GarminCookies = crypto_.Decrypt(e.GarminSsoId, e.GarminCookies);
    e.StravaPassword = crypto_.Decrypt(e.StravaUsername, e.StravaPassword);
    e.StravaCookies = crypto_.Decrypt(e.StravaUsername, e.StravaCookies);

    return e.MapModel();
  }
}