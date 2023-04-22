using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Dauer.Ui.Services;

[SupportedOSPlatform("browser")]
public partial class WebStorage
{
  public const string ModuleName = $"{nameof(Dauer)}.{nameof(Ui)}.{nameof(Services)}.{nameof(WebStorage)}";

  [JSImport("setLocalStorage", ModuleName)]
  public static partial void SetLocalStorage(string key, string value);

  [JSImport("getLocalStorage", ModuleName)]
  public static partial string GetLocalStorage(string key);

  [JSImport("setMessage", ModuleName)]
  public static partial string SetMessage();

  [JSImport("openFile", ModuleName)]
  public static partial Task<JSObject> OpenFileAsync();

  [JSImport("downloadByteArray", ModuleName)]
  public static partial void DownloadByteArray(string fileName, byte[] bytes);

  [JSExport]
  internal static string GetMessage() => $"Hello from {ModuleName}";
}
