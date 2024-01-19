using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace FitEdit.Ui.Browser.Adapters;

[SupportedOSPlatform("browser")]
public partial class WebConsoleAdapter
{
  public const string ModuleName = $"{nameof(FitEdit)}.{nameof(Ui)}.{nameof(Browser)}.{nameof(Adapters)}.{nameof(WebConsoleAdapter)}";

  [JSImport("globalThis.console.log")]
  public static partial void Log(string? message);

  [JSImport("setMessage", ModuleName)]
  public static partial string SetMessage();

  /// <summary>
  /// Called from JavaScript, console.js
  /// </summary>
  [JSExport]
  internal static string GetMessage() => $"{ModuleName} test";
}