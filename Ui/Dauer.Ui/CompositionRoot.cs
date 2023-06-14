using Avalonia.Controls.ApplicationLifetimes;
using Dauer.Services;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Storage;
using Dauer.Ui.Infra.Adapters.Windowing;
using Dauer.Ui.ViewModels;

namespace Dauer.Ui;

public class CompositionRoot
{
  private readonly IApplicationLifetime? lifetime_;

  public IContainer Container { get; set; } = new Container();

  /// <summary>
  /// Don't use this if at all possible. Used as a wrapper for Avalonia's static service locator.
  /// </summary>
  public static IContainer ServiceLocator { get; } = new Container();

  private IStorageAdapter Storage_ => true switch
  {
    _ when lifetime_.IsDesktop(out _) => new DesktopStorageAdapter(),
    _ when lifetime_.IsMobile(out _) => new MobileStorageAdapter(),
    _ => new NullStorageAdapter(),
  };

  private IWindowAdapter Window_ => true switch
  {
    _ when lifetime_.IsDesktop(out _) => new DesktopWindowAdapter(),
    _ when lifetime_.IsMobile(out _) => new MobileWindowAdapter(),
    _ => new NullWindowAdapter(),
  };

  public CompositionRoot(IApplicationLifetime? lifetime)
  {
    lifetime_ = lifetime;
  }

  static CompositionRoot()
  {
    // Not necessary; Console.WriteLine already writes to web browser console
    //if (OperatingSystem.IsBrowser())
    //{
    //  Log.Sinks.Add(Adapters.WebConsoleAdapter.Log);
    //}
  }

  public CompositionRoot Build()
  {
    Container = new Container();

    var log = new LogViewModel();

    var auth = ServiceLocator.Get<IWebAuthenticator>() ?? new NullWebAuthenticator();
    var window = ServiceLocator.Get<IWindowAdapter>() ?? Window_;
    var storage = ServiceLocator.Get<IStorageAdapter>() ?? Storage_;

    var vm = new MainViewModel(
      new FitService(),
      window,
      new PlotViewModel(),
      new LapViewModel(),
      new RecordViewModel(),
      new MapViewModel(),
      new FileViewModel(storage, auth, log),
      log
    );

    Container.Register<IMainViewModel>(vm);
    return this;
  }
}
