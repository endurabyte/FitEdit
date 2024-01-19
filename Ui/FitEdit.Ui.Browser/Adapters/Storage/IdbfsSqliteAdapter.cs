using FitEdit.Adapters.Sqlite;
using FitEdit.Model;
using FitEdit.Model.Extensions;

namespace FitEdit.Ui.Browser.Adapters.Storage;

/// <summary>
/// Manual sync of IndexedDB File System (IDBFS) via JavaScript. Each sync saves the whole in-memory to to disk.
/// https://www.thinktecture.com/blazor/ef-core-and-sqlite-in-browser/
/// </summary>
public class IdbfsSqliteAdapter : SqliteAdapter
{
  public IdbfsSqliteAdapter(string dbPath) : base(dbPath)
  {
  }

  public override async Task<bool> InsertAsync(FitEdit.Model.FileReference t)
  {
    Log.Info($"{nameof(IdbfsSqliteAdapter)}.{nameof(InsertAsync)}({t})");

    var ret = await base.InsertAsync(t).AnyContext();

    await WebStorageAdapterImpl.SyncDb(false).AnyContext();

    return ret;
  }
}
