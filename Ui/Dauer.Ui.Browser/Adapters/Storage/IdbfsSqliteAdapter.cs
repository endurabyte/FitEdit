using Dauer.Adapters.Sqlite;
using Dauer.Model;
using Dauer.Model.Extensions;

namespace Dauer.Ui.Browser.Adapters.Storage;

/// <summary>
/// Manual sync of IndexedDB File System (IDBFS) via JavaScript. Each sync saves the whole in-memory to to disk.
/// https://www.thinktecture.com/blazor/ef-core-and-sqlite-in-browser/
/// </summary>
public class IdbfsSqliteAdapter : SqliteAdapter
{
  public IdbfsSqliteAdapter(string dbPath) : base(dbPath)
  {
  }

  public override async Task<bool> InsertAsync(BlobFile t)
  {
    Log.Info($"{nameof(IdbfsSqliteAdapter)}.{nameof(InsertAsync)}({t})");

    var ret = await base.InsertAsync(t).AnyContext();

    await WebStorageAdapterImpl.SyncDb(false).AnyContext();

    return ret;
  }
}
