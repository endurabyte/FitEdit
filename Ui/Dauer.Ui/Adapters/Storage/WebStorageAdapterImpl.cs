using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Dauer.Ui.Adapters.Storage;

[SupportedOSPlatform("browser")]
public partial class WebStorageAdapterImpl
{
  public const string ModuleName = $"{nameof(Dauer)}.{nameof(Ui)}.{nameof(Adapters)}.{nameof(Storage)}.{nameof(WebStorageAdapterImpl)}";

  [JSImport("setLocalStorage", ModuleName)]
  public static partial void SetLocalStorage(string key, string value);

  [JSImport("getLocalStorage", ModuleName)]
  public static partial string GetLocalStorage(string key);

  [JSImport("openFile", ModuleName)]
  public static partial Task<JSObject> OpenFileAsync();

  [JSImport("downloadByteArray", ModuleName)]
  public static partial void DownloadByteArray(string fileName, byte[] bytes);
}
