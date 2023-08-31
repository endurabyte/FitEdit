using Avalonia;
using Avalonia.Markup.Xaml;
using Dauer.Ui.Infra;
using Dauer.Ui.ViewModels;
using Dauer.Ui.Views;

namespace Dauer.Ui;

public partial class App : Application
{
  public static ICompositionRoot? Root { get; set; }

  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
    StyleExtensions.LoadStyles();
  }

  public override void OnFrameworkInitializationCompleted()
  {
    if (Root is null) { return; }

    Root.RegisterModule(new DauerModule(ApplicationLifetime, Root.Config));
    Root.RegisterModule(new UiModule());

    object? dataContext = Root
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