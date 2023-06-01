using System.Reactive.Subjects;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Dauer.Ui.Adapters.Windowing;

[SupportedOSPlatform("browser")]
public partial class WebWindowAdapterImpl
{
  public const string ModuleName = $"{nameof(Dauer)}.{nameof(Ui)}.{nameof(Adapters)}.{nameof(Windowing)}.{nameof(WebWindowAdapterImpl)}";

  public static IObservable<(double, double)> Resized => resizedSubject_;
  private static readonly ISubject<(double, double)> resizedSubject_ = new Subject<(double, double)>();

  /// <summary>
  /// Subscribe to the browser window resize event and call <see cref="NotifyWindowResized(double, double)"/> when it happens.
  /// </summary>
  [JSImport("listenForResize", ModuleName)]
  public static partial void ListenForResize();

  /// <summary>
  /// Called from JavaScript, windowing.js
  /// </summary>
  [JSExport]
  public static void NotifyWindowResized(double width, double height) => resizedSubject_.OnNext((width, height));
}
