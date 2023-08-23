using Avalonia.Controls;
using Avalonia.Threading;
using Dauer.Ui.ViewModels;
using ReactiveUI;

namespace Dauer.Ui.Views;

public partial class MainView : UserControl
{
  private readonly RowDefinitions defaultRowDefinitions_;

  public MainView()
  {
    InitializeComponent();

    defaultRowDefinitions_ = new RowDefinitions(MainGrid.RowDefinitions.ToString());
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

    vm.ObservableForProperty(x => x.IsSmallDisplay).Subscribe(_ => RespondToDisplaySize(vm));
    RespondToDisplaySize(vm);
  }

  private void RespondToDisplaySize(IMainViewModel vm)
  {
    MainGrid.RowDefinitions.Clear();

    if (vm.IsSmallDisplay)
    {
      MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
      MainGrid.Children.Remove(GridSplitter);
      MainGrid.Children.Remove(ChartGrid);
    }
    else
    {
      foreach (var def in defaultRowDefinitions_)
      {
        MainGrid.RowDefinitions.Add(def);
      }

      if (GridSplitter.Parent == null) { MainGrid.Children.Add(GridSplitter); }
      if (ChartGrid.Parent == null) { MainGrid.Children.Add(ChartGrid); }

      // Select tab before PlotTab, since PlotTab is now hidden
      int i = MainTabControl.Items.IndexOf(PlotTab);
      MainTabControl.SelectedIndex = Math.Min(MainTabControl.SelectedIndex, i - 1);
    }
  }
}