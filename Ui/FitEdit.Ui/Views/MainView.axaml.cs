using Avalonia.Controls;
using Avalonia.Threading;
using FitEdit.Ui.ViewModels;
using ReactiveUI;

namespace FitEdit.Ui.Views;

public partial class MainView : UserControl
{
  private readonly RowDefinitions defaultRowDefinitions_;

  /// <summary>
  /// Index of the selected tab in landscape display mode, 
  /// for example on mobile devices in landscape mode or desktops/laptops when the window height < width
  /// </summary>
  private int landscapeTabIndex_;

  public MainView()
  {
    InitializeComponent();

    defaultRowDefinitions_ = new RowDefinitions(MainGrid.RowDefinitions.ToString());
    DataContextChanged += HandleDataContextChanged;
    MainTabControl.ObservableForProperty(x => x.SelectedIndex).Subscribe(_ =>
    {
      if (DataContext is not IMainViewModel vm) { return; }

      // Remember which tab was selected in landscape mode.
      // When we leave portrait mode, we'll jump back to it.
      if (!vm.IsCompact)
      {
        landscapeTabIndex_ = MainTabControl.SelectedIndex;
      }
    });
  }

  private void HandleDataContextChanged(object? sender, EventArgs e)
  {
    if (DataContext is not IMainViewModel vm) { return; }

    // Show the map if it has coordinates, else hide it.
    vm.Map.ObservableForProperty(x => x.HasCoordinates).Subscribe(async x =>
    {
      if (vm.IsCompact) { return; }
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        var value = x.Value ? GridLength.Star : new GridLength(0);
        ChartGrid.ColumnDefinitions[2].Width = value;
      });
    });

    vm.ObservableForProperty(x => x.IsCompact).Subscribe(_ => RespondToDisplaySize(vm));
    RespondToDisplaySize(vm);
  }

  private void RespondToDisplaySize(IMainViewModel vm)
  {
    MainGrid.RowDefinitions.Clear();

    if (vm.IsCompact)
    {
      HideChartAndMap();
      return;
    }

    // We just left portrait mode. Jump back to last tab that was selected in landscape mode.
    ShowChartAndMap();
  }

  private void HideChartAndMap()
  {
    MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
    MainGrid.Children.Remove(GridSplitter);
    MainGrid.Children.Remove(ChartGrid);
  }

  private void ShowChartAndMap()
  {
    MainTabControl.SelectedIndex = landscapeTabIndex_;

    foreach (var def in defaultRowDefinitions_)
    {
      MainGrid.RowDefinitions.Add(def);
    }

    if (GridSplitter.Parent == null) { MainGrid.Children.Add(GridSplitter); }
    if (ChartGrid.Parent == null) { MainGrid.Children.Add(ChartGrid); }
  }
}