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
    if (CompositionRoot.Instance == null) { return; }

    object? dataContext = CompositionRoot.Instance
      .Build(ApplicationLifetime)
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