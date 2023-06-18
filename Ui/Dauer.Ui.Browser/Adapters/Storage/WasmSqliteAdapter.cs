using Dauer.Adapters.Sqlite;
using Dauer.Model;
using Dauer.Model.Extensions;

namespace Dauer.Ui.Browser.Adapters.Storage;

public class WasmSqliteAdapter : SqliteAdapter
{
  public WasmSqliteAdapter(string dbPath) : base(dbPath)
  {
  }

  public override async Task<bool> InsertAsync(BlobFile t)
  {
    Log.Info($"{nameof(WasmSqliteAdapter)}.{nameof(InsertAsync)}({t})");

    var ret = await base.InsertAsync(t).AnyContext();
    await WebStorageAdapterImpl.SyncDb(false).AnyContext();
    return ret;
  }
}
