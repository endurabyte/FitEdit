using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Dauer.Ui.Services;

[SupportedOSPlatform("browser")]
public partial class WebConsole
{
  public const string ModuleName = $"{nameof(Dauer.Ui)}.{nameof(Services)}.{nameof(WebConsole)}";

  [JSImport("globalThis.console.log")]
  public static partial void Log(string message);
}