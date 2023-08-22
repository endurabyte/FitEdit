using Avalonia.Controls;
using Avalonia.Threading;
using Dauer.Ui.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Views;

public partial class MainView : UserControl
{
  private readonly RowDefinitions large_ = new("2*, Auto, *");
  private readonly RowDefinitions small_ = new("*");

  [Reactive] public int GridSplitterRow { get; set; } = 1;
  [Reactive] public int ChartGridRow { get; set; } = 2;

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
      if (vm.IsSmallDisplay) { return; }
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        var value = x.Value ? GridLength.Star : new GridLength(0);
        ChartGrid.ColumnDefinitions[2].Width = value;
      });
    });

    vm.ObservableForProperty(x => x.IsSmallDisplay).Subscribe(_ =>
    {
      // Prevent grid row index exception
      Grid.SetRow(GridSplitter, vm.IsSmallDisplay ? 0 : 1);
      Grid.SetRow(ChartGrid, vm.IsSmallDisplay ? 0 : 2);

      MainGrid.RowDefinitions = vm.IsSmallDisplay ? small_ : large_;

      // Select tab before PlotTab, since PlotTab is now hidden
      if (!vm.IsSmallDisplay)
      {
        int i = MainTabControl.Items.IndexOf(PlotTab);
        MainTabControl.SelectedIndex = Math.Min(MainTabControl.SelectedIndex, i - 1);
      }
    });
  }
}