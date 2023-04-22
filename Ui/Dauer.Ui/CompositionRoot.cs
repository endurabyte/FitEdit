using Avalonia.Controls.ApplicationLifetimes;
using Dauer.Ui.Adapters;
using Dauer.Ui.Adapters.Storage;
using Dauer.Ui.Services;
using Dauer.Ui.ViewModels;

namespace Dauer.Ui;

public class CompositionRoot
{
  private readonly IApplicationLifetime? lifetime_;

  private IStorageAdapter Storage_ => OperatingSystem.IsBrowser() switch
  {
    true => new WebStorageAdapter(),
    false when lifetime_.IsDesktop(out _) => new DesktopStorageAdapter(),
    false when lifetime_.IsMobile(out _) => new MobileStorageAdapter(),
    _ => new NullStorageAdapter(),
  };

  private readonly Dictionary<Type, object> registrations_ = new();

  public CompositionRoot(IApplicationLifetime? lifetime)
  {
    lifetime_ = lifetime;

    var db = new Database();
    var vm = new MainViewModel(Storage_, db);

    registrations_[typeof(IMainViewModel)] = vm;
  }

  public T Get<T>() => (T)registrations_[typeof(T)];
}
