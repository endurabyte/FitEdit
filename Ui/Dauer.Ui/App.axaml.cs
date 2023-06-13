using Avalonia;
using Avalonia.Markup.Xaml;
using Dauer.Model;
using Dauer.Ui.Infra;
using Dauer.Ui.ViewModels;
using Dauer.Ui.Views;

namespace Dauer.Ui;

public partial class App : Application
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    Log.Level = LogLevel.Info;

    var root = new CompositionRoot(ApplicationLifetime).Build();
    object? dataContext = root.Container.Get<IMainViewModel>();

    if (ApplicationLifetime.IsDesktop(out var desktop))
    {
      desktop!.MainWindow = new MainWindow
      {
        DataContext = dataContext
      };
    }

    else if (ApplicationLifetime.IsMobile(out var mobile))
    {
      mobile!.MainView = new MainView
      {
        DataContext = dataContext
      };
    }

    base.OnFrameworkInitializationCompleted();
  }
}