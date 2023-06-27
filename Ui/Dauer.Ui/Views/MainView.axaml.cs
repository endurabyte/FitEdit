using Avalonia.Controls;
using Avalonia.Threading;
using Dauer.Ui.ViewModels;
using ReactiveUI;

namespace Dauer.Ui.Views;

public partial class MainView : UserControl
{
  public MainView()
  {
    InitializeComponent();

    DataContextChanged += HandleDataContextChanged;
  }

  private void HandleDataContextChanged(object? sender, EventArgs e)
  {
    if (DataContext is not IMainViewModel vm)
    {
      return;
    }

    // Show the map if it has coordinates, else hide it.
    vm.Map.ObservableForProperty(x => x.HasCoordinates).Subscribe(async x =>
    {
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        var value = x.Value ? GridLength.Star : new GridLength(0);
        ChartGrid.RowDefinitions[2].Height = value;
      });
    });
  }
}