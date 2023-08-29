using Avalonia.Controls;
using Avalonia.Threading;
using Dauer.Ui.ViewModels;
using ReactiveUI;

namespace Dauer.Ui.Views;

public partial class MainView : UserControl
{
  private readonly RowDefinitions defaultRowDefinitions_;

  /// <summary>
  /// Index of the selected tab in portrait display mode, 
  /// for example on mobile devices in portrait mode or desktops/laptops when the window height > width
  /// </summary>
  private int portraitTabIndex = -1;

  /// <summary>
  /// Index of the selected tab in landscape display mode, 
  /// for example on mobile devices in landscape mode or desktops/laptops when the window height < width
  /// </summary>
  private int landscapeTabIndex = -1;

  public MainView()
  {
    InitializeComponent();

    defaultRowDefinitions_ = new RowDefinitions(MainGrid.RowDefinitions.ToString());
    DataContextChanged += HandleDataContextChanged;
    MainTabControl.ObservableForProperty(x => x.SelectedIndex).Subscribe(_ =>
    {
      if (DataContext is not IMainViewModel vm) { return; }
      if (vm.IsPortrait) { portraitTabIndex = MainTabControl.SelectedIndex; }
      else { landscapeTabIndex = MainTabControl.SelectedIndex; }
    });
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
      if (vm.IsPortrait) { return; }
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        var value = x.Value ? GridLength.Star : new GridLength(0);
        ChartGrid.ColumnDefinitions[2].Width = value;
      });
    });

    vm.ObservableForProperty(x => x.IsPortrait).Subscribe(_ => RespondToDisplaySize(vm));
    RespondToDisplaySize(vm);
  }

  private void RespondToDisplaySize(IMainViewModel vm)
  {
    MainGrid.RowDefinitions.Clear();

    bool portraitViewWasOnPlotOrMap = 
         portraitTabIndex == MainTabControl.Items.IndexOf(PlotTab) 
      || portraitTabIndex == MainTabControl.Items.IndexOf(MapTab);

    // Jump back to plot or map tab if going back to portrait
    MainTabControl.SelectedIndex = vm.IsPortrait && portraitViewWasOnPlotOrMap
      ? portraitTabIndex
      : landscapeTabIndex;

    if (vm.IsPortrait)
    {
      MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
      MainGrid.Children.Remove(GridSplitter);
      MainGrid.Children.Remove(ChartGrid);
      return;
    }

    foreach (var def in defaultRowDefinitions_)
    {
      MainGrid.RowDefinitions.Add(def);
    }

    if (GridSplitter.Parent == null) { MainGrid.Children.Add(GridSplitter); }
    if (ChartGrid.Parent == null) { MainGrid.Children.Add(ChartGrid); }
  }
}