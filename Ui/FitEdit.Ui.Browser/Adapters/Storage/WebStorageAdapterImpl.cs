using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace FitEdit.Ui.Browser.Adapters.Storage;

[SupportedOSPlatform("browser")]
public partial class WebStorageAdapterImpl
{
  public const string ModuleName = $"{nameof(FitEdit)}.{nameof(Ui)}.{nameof(Browser)}.{nameof(Adapters)}.{nameof(Storage)}.{nameof(WebStorageAdapterImpl)}";

  [JSImport("setLocalStorage", ModuleName)]
  public static partial void SetLocalStorage(string key, string value);

  [JSImport("getLocalStorage", ModuleName)]
  public static partial string GetLocalStorage(string key);

  [JSImport("openFile", ModuleName)]
  public static partial Task<JSObject> OpenFileAsync();

  [JSImport("downloadByteArray", ModuleName)]
  public static partial void DownloadByteArray(string fileName, byte[] bytes);

  [JSImport("mountAndInitializeDb", ModuleName)]
  public static partial Task MountAndInitializeDb();

  /// <summary>
  /// Save or load the database to persistent storage
  /// See https://emscripten.org/docs/api_reference/Filesystem-API.html#filesystem-api-idbfs
  /// </summary>
  /// <param name="populate">true: load from file into memory. false: load from memory into file</param>
  [JSImport("syncDb", ModuleName)]
  public static partial Task SyncDb(bool populate);
}
