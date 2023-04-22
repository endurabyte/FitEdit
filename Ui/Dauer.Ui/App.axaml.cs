using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Dauer.Ui.Services;
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
    var db = new Database();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      desktop.MainWindow = new MainWindow
      {
        DataContext = new MainViewModel(db)
      };
    }
    else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
    {
      singleViewPlatform.MainView = new MainView
      {
        DataContext = new MainViewModel(db)
      };
    }

    base.OnFrameworkInitializationCompleted();
  }
}