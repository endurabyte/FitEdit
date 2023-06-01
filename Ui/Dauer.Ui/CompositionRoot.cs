using Avalonia.Controls.ApplicationLifetimes;
using Dauer.Services;
using Dauer.Ui.Adapters.Storage;
using Dauer.Ui.Adapters.Windowing;
using Dauer.Ui.ViewModels;

namespace Dauer.Ui;

public class CompositionRoot
{
  private readonly IApplicationLifetime? lifetime_;

  public IContainer Container { get; set; } = new Container();

  /// <summary>
  /// Don't use this if at all possible. Used as a wrapper for Avalonia's static service locator.
  /// </summary>
  public static IContainer ServiceLocator { get; } = new AvaloniaContainer();

  private IStorageAdapter Storage_ => OperatingSystem.IsBrowser() switch
  {
    true => new WebStorageAdapter(),
    false when lifetime_.IsDesktop(out _) => new DesktopStorageAdapter(),
    false when lifetime_.IsMobile(out _) => new MobileStorageAdapter(),
    _ => new NullStorageAdapter(),
  };

  private IWindowAdapter Window_ => OperatingSystem.IsBrowser() switch
  {
    true => new WebWindowAdapter(),
    false when lifetime_.IsDesktop(out _) => new DesktopWindowAdapter(),
    false when lifetime_.IsMobile(out _) => new MobileWindowAdapter(),
    _ => new NullWindowAdapter(),
  };

  public CompositionRoot(IApplicationLifetime? lifetime)
  {
    lifetime_ = lifetime;
  }

  public CompositionRoot Build()
  {
    Container = new Container();

    var vm = new MainViewModel(
      Storage_,
      Window_,
      new FitService(),
      new PlotViewModel(),
      new LapViewModel(),
      new RecordViewModel(),
      new MapViewModel(),
      ServiceLocator.Get<IWebAuthenticator>() ?? new NullWebAuthenticator());

    Container.Register<IMainViewModel, MainViewModel>(vm);
    return this;
  }
}
