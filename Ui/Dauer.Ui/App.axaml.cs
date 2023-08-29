using Avalonia;
using Avalonia.Markup.Xaml;
using Dauer.Ui.Infra;
using Dauer.Ui.ViewModels;
using Dauer.Ui.Views;

namespace Dauer.Ui;

public partial class App : Application
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
    StyleExtensions.LoadStyles();
  }

  public override void OnFrameworkInitializationCompleted()
  {
    var root = CompositionRoot.Create();
    root.RegisterModule(new DauerModule(ApplicationLifetime, root.Config));
    root.RegisterModule(new UiModule());

    object? dataContext = root
      .Build()
      .Get<IMainViewModel>();

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