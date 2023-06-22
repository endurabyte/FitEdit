using System.Reactive.Subjects;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Dauer.Ui.Browser.Adapters.Windowing;

[SupportedOSPlatform("browser")]
public partial class WebWindowAdapterImpl
{
  public const string ModuleName = $"{nameof(Dauer)}.{nameof(Ui)}.{nameof(Browser)}.{nameof(Adapters)}.{nameof(Windowing)}.{nameof(WebWindowAdapterImpl)}";

  public static IObservable<(double, double)> Resized => resizedSubject_;
  private static readonly ISubject<(double, double)> resizedSubject_ = new Subject<(double, double)>();

  public static IObservable<string> MessageReceived => messageReceived_;
  private static readonly ISubject<string> messageReceived_ = new Subject<string>();

  /// <summary>
  /// Subscribe to the browser window resize event and call <see cref="NotifyWindowResized(double, double)"/> when it happens.
  /// </summary>
  [JSImport("listenForResize", ModuleName)]
  public static partial void ListenForResize();

  /// <summary>
  /// Subscribe to window messages e.g. from child windows and call <see cref="NotifyWindowMessageReceived(string)"/> when it happens.
  /// </summary>
  [JSImport("listenForMessages", ModuleName)]
  public static partial void ListenForMessages();
  
  [JSImport("openWindow", ModuleName)]
  public static partial void OpenWindow(string url, string windowName);

  [JSImport("closeWindow", ModuleName)]
  public static partial void CloseWindow(string windowName);

  [JSImport("getOrigin", ModuleName)]
  public static partial string GetOrigin();

  /// <summary>
  /// Called from JavaScript, windowing.js
  /// </summary>
  [JSExport]
  public static void NotifyWindowResized(double width, double height) => resizedSubject_.OnNext((width, height));

  /// <summary>
  /// Called from JavaScript, windowing.js
  /// </summary>
  [JSExport]
  public static void NotifyMessageReceived(string message) => messageReceived_.OnNext(message);
}
